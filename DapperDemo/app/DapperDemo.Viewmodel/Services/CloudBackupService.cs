using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Services;

namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// Decides when an automatic backup is due, and runs one.
/// </summary>
/// <remarks>
/// The archive is built into a temporary file and then uploaded from it, rather than written
/// straight at the destination. A cloud upload wants a length up front and may have to start over
/// after a dropped connection, neither of which a stream being zipped as it goes can offer.
/// </remarks>
public sealed class CloudBackupService(BackupArchive archive, CloudBackupStore store, CloudBackupState state)
{
    /// <summary>
    /// The single name every automatic backup is stored under, so each run replaces the last.
    /// </summary>
    /// <remarks>
    /// Undated on purpose: one file that is always the newest, rather than a folder that grows by
    /// a full database and every photo each week.
    /// </remarks>
    private const string ArchiveName = "patas-backup.zip";

    private BackupArchive Archive { get; } = archive;

    private CloudBackupStore Store { get; } = store;

    private CloudBackupState State { get; } = state;

    /// <summary>Gets the chosen folder's name, or null when none is set up or reachable.</summary>
    /// <returns>The folder's display name, or null.</returns>
    public Task<string?> DestinationNameAsync() => Store.DestinationNameAsync();

    /// <summary>Gets a value indicating whether a destination is set up and writable right now.</summary>
    /// <returns>True when a backup could be written.</returns>
    public Task<bool> IsLinkedAsync() => Store.IsLinkedAsync();

    /// <summary>
    /// Asks the user which folder backups should go to, and remembers it.
    /// </summary>
    /// <remarks>
    /// The one setup step. Everything after it is automatic: the daily run uses the same
    /// destination, and on Android the stored bookmark carries the permission grant so later
    /// launches write there without asking again.
    /// </remarks>
    /// <returns>Successful, or Failed if the user cancelled or the choice could not be kept.</returns>
    public Task<Response> LinkAsync() => Store.LinkAsync();

    /// <summary>Gets when a backup last reached the destination.</summary>
    /// <returns>The time in UTC, or null if none ever has.</returns>
    public async Task<DateTime?> LastUploadAsync()
    {
        var schedule = await State.ReadAsync().NoSync();
        return schedule.LastUploadUtc;
    }

    /// <summary>
    /// Gets a value indicating whether today's copy is still owed.
    /// </summary>
    /// <remarks>
    /// Local time, not UTC: the schedule is a time of day on the sitter's clock — see
    /// <see cref="CloudBackupSchedule.RunAt"/>.
    /// </remarks>
    /// <returns>True when no backup has been taken since the last scheduled run.</returns>
    public async Task<bool> IsDueAsync()
    {
        var schedule = await State.ReadAsync().NoSync();
        return schedule.IsDue(DateTime.Now);
    }

    /// <summary>
    /// Builds an archive and sends it to the destination, replacing the one already there.
    /// </summary>
    /// <returns>Successful, or Failed if the archive could not be built or the upload did not land.</returns>
    public async Task<Response> RunAsync()
    {
        var temporary = Path.Combine(
            Path.GetTempPath(),
            "dapperdemo-cloud-" + Guid.NewGuid().ToString("N") + ".zip");

        try
        {
            Response written;
            var file = File.Create(temporary);
            await using (file.ConfigureAwait(false))
            {
                written = await Archive.WriteToAsync(file).NoSync();
            }

            if (written != Response.Successful)
            {
                return written;
            }

            Response uploaded;
            var content = File.OpenRead(temporary);
            await using (content.ConfigureAwait(false))
            {
                uploaded = await Store.UploadAsync(content, ArchiveName).NoSync();
            }

            // The attempt is recorded either way, the copy only when it landed. A failed run
            // therefore stays owed — it must, or one bad morning silently becomes a day with no
            // backup — while the attempt stamp keeps the retry to once an hour instead of once
            // every check. See CloudBackupSchedule.RetryAfter.
            var now = DateTime.UtcNow;
            var schedule = await State.ReadAsync().NoSync();
            await State.WriteAsync(schedule with
            {
                LastUploadUtc = uploaded == Response.Successful ? now : schedule.LastUploadUtc,
                LastAttemptUtc = now,
            }).NoSync();

            return uploaded;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
        finally
        {
            TryDelete(temporary);
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
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
        }
    }
}