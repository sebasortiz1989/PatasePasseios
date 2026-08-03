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

    private DapperDatabaseService Database { get; } = database;

    /// <summary>A dated file name to offer in the save dialog.</summary>
    public static string SuggestedFileName() =>
        "patas-backup-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".zip";

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
    /// <param name="source">The archive. Copied to a temp file first, so a non-seekable stream is fine.</param>
    /// <returns>Successful, or Failed when the file is not a usable backup — in which case nothing was touched.</returns>
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

            if (!extracted || !await IsUsableDatabaseAsync(candidate).ConfigureAwait(false))
            {
                return Response.Failed;
            }

            // Only now is the archive known good, so this is the first thing that touches the
            // user's own data.
            await Task.Run(() =>
            {
                File.Copy(candidate, Database.DatabasePath, overwrite: true);

                using var zip = ZipFile.OpenRead(localCopy);
                RestoreImages(zip);
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
    /// Opens the extracted file and checks it carries the tables this app expects, so a restore
    /// cannot overwrite good data with an unrelated or truncated zip.
    /// </summary>
    private static async Task<bool> IsUsableDatabaseAsync(string path)
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
            return present.Contains("PetSitter") && present.Contains("Dogs") && present.Contains("Tutors");
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return false;
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