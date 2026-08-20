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

    /// <summary>Gets the destination's name as the user should see it.</summary>
    public string DestinationName => Store.DisplayName;

    private BackupArchive Archive { get; } = archive;

    private CloudBackupStore Store { get; } = store;

    private CloudBackupState State { get; } = state;

    /// <summary>Gets when a backup last reached the destination.</summary>
    /// <returns>The time in UTC, or null if none ever has.</returns>
    public async Task<DateTime?> LastUploadAsync()
    {
        var schedule = await State.ReadAsync().NoSync();
        return schedule.LastUploadUtc;
    }

    /// <summary>Gets a value indicating whether the user should be asked to back up now.</summary>
    /// <returns>True when a backup is overdue and the last prompt is old enough to repeat.</returns>
    public async Task<bool> IsDueAsync()
    {
        var schedule = await State.ReadAsync().NoSync();
        return schedule.IsDue(DateTime.UtcNow);
    }

    /// <summary>
    /// Records that the user was asked and said no, so they are not asked again straight away.
    /// </summary>
    /// <returns>A task that completes once the answer is stored.</returns>
    public async Task DeferAsync()
    {
        var schedule = await State.ReadAsync().NoSync();
        await State.WriteAsync(schedule with { LastPromptUtc = DateTime.UtcNow }).NoSync();
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

            // The prompt stamp moves on either outcome, but the upload stamp only on success — a
            // failed run must stay overdue, or one bad week of connectivity silently becomes a
            // month without a backup.
            var schedule = await State.ReadAsync().NoSync();
            await State.WriteAsync(schedule with
            {
                LastUploadUtc = uploaded == Response.Successful ? DateTime.UtcNow : schedule.LastUploadUtc,
                LastPromptUtc = DateTime.UtcNow,
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