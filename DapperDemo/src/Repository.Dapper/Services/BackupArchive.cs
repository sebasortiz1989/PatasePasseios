using Dapper;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO.Compression;

namespace DapperDemo.Repository.Dapper.Services;

/// <summary>
/// Packs everything the app owns into one zip, and puts it back.
/// </summary>
/// <remarks>
/// A zip rather than the bare database file because a dog's photo is not in the database — the
/// Dogs row holds a file name and the image sits beside it in <see cref="DogImageStore"/>. Copying
/// only the .db would restore every record and lose every photo, which looks like success right up
/// until someone opens a dog.
/// </remarks>
public sealed class BackupArchive(DapperDatabaseService database)
{
    /// <summary>Name of the database inside the archive.</summary>
    private const string DatabaseEntry = "DapperDemo.db";

    /// <summary>Folder prefix the dog photos are stored under inside the archive.</summary>
    private const string ImagesPrefix = "DogImages/";

    private const string ManifestEntry = "backup.json";

    /// <summary>
    /// What the database being replaced is kept as, beside the live one.
    /// </summary>
    /// <remarks>
    /// A restore is the only thing in the app that discards every record at once, and until this
    /// existed it did so with nothing to go back to. One file, overwritten by the next restore:
    /// the point is to survive picking the wrong archive, not to keep a history.
    /// </remarks>
    private const string ReplacedSuffix = ".replaced";

    private DapperDatabaseService Database { get; } = database;

    /// <summary>
    /// A dated file name to offer in the save dialog, without extension — the dialog appends the
    /// one that matches what is being written.
    /// </summary>
    public static string SuggestedFileName() =>
        "patas-backup-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes the archive to <paramref name="destination"/>, which the caller owns and disposes.
    /// </summary>
    public async Task<Response> WriteToAsync(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var snapshot = Path.Combine(Path.GetTempPath(), "dapperdemo-backup-" + Guid.NewGuid().ToString("N") + ".db");

        try
        {
            // VACUUM INTO rather than File.Copy: it asks SQLite for a consistent snapshot, so the
            // archive cannot catch the file mid-write or miss a journal that has not been folded
            // back in yet.
            using (var connection = Database.Connection)
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync("VACUUM INTO @Path", new { Path = snapshot }).ConfigureAwait(false);
            }

            // The compression APIs are synchronous with no async counterpart, so the work goes to
            // the thread pool rather than blocking the caller — on a phone this is a database plus
            // every dog photo, and the caller is the UI thread.
            await Task.Run(() =>
            {
                using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

                zip.CreateEntryFromFile(snapshot, DatabaseEntry, CompressionLevel.Optimal);

                var imagesFolder = DogImageStore.Folder;
                if (Directory.Exists(imagesFolder))
                {
                    foreach (var file in Directory.GetFiles(imagesFolder))
                    {
                        // Photos are already JPEG, so compressing them again only costs time.
                        zip.CreateEntryFromFile(file, ImagesPrefix + Path.GetFileName(file), CompressionLevel.NoCompression);
                    }
                }

                WriteManifest(zip);
            }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (Exception e) when (e is IOException or SqliteException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    /// <summary>
    /// Replaces the current database and photos with the archive's. Everything currently stored is
    /// discarded, which is the point: a restore is "make this device look like that backup".
    /// </summary>
    /// <remarks>
    /// The database being replaced is copied aside first — see <see cref="ReplacedSuffix"/>. The
    /// archive is checked before anything is touched, so this is not about a corrupt file; it is
    /// about the user picking last month's backup by mistake, which nothing else here can undo.
    /// </remarks>
    /// <param name="source">The archive. Copied to a temp file first, so a non-seekable stream is fine.</param>
    /// <returns>
    /// Successful; IncompatibleVersion when the archive's schema does not match this build's; or
    /// Failed when the file is not one of this app's backups. In either failing case nothing on
    /// the device was touched.
    /// </returns>
    public async Task<Response> RestoreFromAsync(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var work = Path.Combine(Path.GetTempPath(), "dapperdemo-restore-" + Guid.NewGuid().ToString("N"));
        var localCopy = work + ".zip";
        var candidate = work + ".db";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localCopy)!);

            // Android hands back a content:// stream that cannot seek, and ZipArchive needs to
            // seek to read the central directory.
            var buffer = File.Create(localCopy);
            await using (buffer.ConfigureAwait(false))
            {
                await source.CopyToAsync(buffer).ConfigureAwait(false);
            }

            // Off the calling thread for the same reason as the export: synchronous compression
            // APIs, potentially a lot of photos.
            var extracted = await Task.Run(() =>
            {
                using var zip = ZipFile.OpenRead(localCopy);
                var databaseEntry = zip.GetEntry(DatabaseEntry);
                if (databaseEntry == null)
                {
                    return false;
                }

                databaseEntry.ExtractToFile(candidate, overwrite: true);
                return true;
            }).ConfigureAwait(false);

            if (!extracted)
            {
                return Response.Failed;
            }

            var usable = await InspectDatabaseAsync(candidate).ConfigureAwait(false);
            if (usable != Response.Successful)
            {
                return usable;
            }

            // Only now is the archive known good, so this is the first thing that touches the
            // user's own data.
            await Task.Run(() =>
            {
                // Every connection this process has opened against the old file is dropped before
                // it is replaced. A pooled connection outlives its using block, and one handed back
                // out after the copy carries SQLite's page cache for a database that no longer
                // exists — which reads as data the restore was supposed to bring in being absent.
                SqliteConnection.ClearAllPools();

                // What is about to be discarded, kept where the user's own data lives rather than
                // in the temporary folder — somewhere it will still be there tomorrow if the
                // archive turns out to have been the wrong one.
                KeepReplacedDatabase(Database.DatabasePath);

                File.Copy(candidate, Database.DatabasePath, overwrite: true);

                // The database this service was initialised against has just been replaced. An
                // archive written before an additive column shipped does not carry it, and the
                // queries name it — so the migration runs again here rather than waiting for the
                // next launch, which is well after the user is back on the agenda.
                Database.ApplyMissingColumns();

                using var zip = ZipFile.OpenRead(localCopy);
                RestoreImages(zip);

                // And again on the way out: the migration above opened its own pooled connection,
                // against a file that is about to be read by the sign-in the user does next.
                SqliteConnection.ClearAllPools();
            }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or SqliteException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
        finally
        {
            TryDelete(localCopy);
            TryDelete(candidate);
        }
    }

    /// <summary>
    /// Opens the extracted file and decides whether it may replace the user's data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two questions, and they fail differently. Are the tables this app expects present — if not
    /// it is an unrelated or truncated zip. And does its <c>PRAGMA user_version</c> match this
    /// build's schema — if not, restoring it is worse than refusing it.
    /// </para>
    /// <para>
    /// A <b>lower</b> version means the file predates a layout change that
    /// <see cref="DapperDatabaseService"/> handles by dropping every table. Accepting it would
    /// report success, then destroy the restored data on the next launch, with nothing left to
    /// try again from. A <b>higher</b> version means an archive from a newer build, whose tables
    /// this build's queries were not written against.
    /// </para>
    /// <para>
    /// The version is read from the database rather than from <c>backup.json</c> on purpose: it is
    /// the same value the launch-time check compares, so the two can never disagree, and it is
    /// present even in an archive written before the manifest existed. The manifest keeps its copy
    /// for anyone reading the zip by hand.
    /// </para>
    /// </remarks>
    /// <param name="path">The extracted database.</param>
    /// <returns>
    /// Successful, IncompatibleVersion when the schema does not match, or Failed when the file is
    /// not one of this app's backups.
    /// </returns>
    private static async Task<Response> InspectDatabaseAsync(string path)
    {
        // Pooling off: a pooled connection keeps the file handle open past Dispose, and the temp
        // file this validates then cannot be deleted — one leaked copy of the database per import.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false,
        }.ToString();

        try
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            var tables = await connection.QueryAsync<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table'").ConfigureAwait(false);

            var present = tables.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!present.Contains("PetSitter") || !present.Contains("Dogs") || !present.Contains("Tutors"))
            {
                return Response.Failed;
            }

            var version = await connection.ExecuteScalarAsync<int>("PRAGMA user_version").ConfigureAwait(false);

            return version == DapperDatabaseService.CurrentSchemaVersion
                ? Response.Successful
                : Response.IncompatibleVersion;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    private static void RestoreImages(ZipArchive zip)
    {
        var folder = DogImageStore.Folder;

        foreach (var stale in Directory.GetFiles(folder))
        {
            TryDelete(stale);
        }

        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.StartsWith(ImagesPrefix, StringComparison.Ordinal) || entry.Length == 0)
            {
                continue;
            }

            // Only the bare name is used, so a crafted archive cannot write outside the folder
            // through a path like DogImages/../../something.
            var name = Path.GetFileName(entry.FullName);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            entry.ExtractToFile(Path.Combine(folder, name), overwrite: true);
        }
    }

    /// <summary>
    /// Synchronous because it runs inside the thread-pool block that builds the archive, and
    /// because a zip entry stream has no async writer worth the ceremony for three lines of JSON.
    /// </summary>
    private static void WriteManifest(ZipArchive zip)
    {
        var manifest = zip.CreateEntry(ManifestEntry, CompressionLevel.Optimal);
        using var stream = manifest.Open();
        using var writer = new StreamWriter(stream);

        writer.Write(
            $$"""
            {
              "app": "DapperDemo",
              "schemaVersion": {{DapperDatabaseService.CurrentSchemaVersion}},
              "createdUtc": "{{DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}}"
            }
            """);
    }

    /// <summary>
    /// Copies the live database aside before a restore writes over it.
    /// </summary>
    /// <remarks>
    /// Best effort: a restore the user asked for must not be refused because the copy could not be
    /// made. The photos are deliberately not copied — they are the bulky half, and the records are
    /// what cannot be reconstructed.
    /// </remarks>
    /// <param name="databasePath">The live database, which is about to be replaced.</param>
    private static void KeepReplacedDatabase(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
            {
                File.Copy(databasePath, databasePath + ReplacedSuffix, overwrite: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine(e);
        }
    }
}