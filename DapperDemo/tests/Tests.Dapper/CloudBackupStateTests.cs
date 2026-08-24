using PatasePasseios.Repository.Dapper;
using PatasePasseios.Repository.Dapper.Services;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The state file the schedule is read from. Driven against a throwaway path rather than the one
/// belonging to whoever runs the tests.
/// </summary>
public class CloudBackupStateTests : IDisposable
{
    private readonly string path = Path.Combine(
        Path.GetTempPath(),
        "patasepasseios-state-tests-" + Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public async Task AMissingFileReadsAsEmpty()
    {
        var state = new CloudBackupState(path);

        Assert.Equal(CloudBackupSchedule.Empty, await state.ReadAsync());
    }

    [Fact]
    public async Task AWrittenScheduleComesBack()
    {
        var state = new CloudBackupState(path);
        var written = new CloudBackupSchedule(
            new DateTime(2026, 8, 12, 9, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 12, 9, 31, 0, DateTimeKind.Utc),
            "bookmark-bytes");

        Assert.Equal(Response.Successful, await state.WriteAsync(written));

        var read = await state.ReadAsync();

        Assert.Equal(written.LastUploadUtc, read.LastUploadUtc);
        Assert.Equal(written.LastAttemptUtc, read.LastAttemptUtc);
        Assert.Equal(written.Destination, read.Destination);
    }

    [Fact]
    public async Task TheAskedStampSurvivesTheRoundTrip()
    {
        var state = new CloudBackupState(path);
        var asked = new DateTime(2026, 8, 19, 7, 15, 0, DateTimeKind.Utc);

        await state.WriteAsync(CloudBackupSchedule.Empty with { LastPromptUtc = asked });

        Assert.Equal(asked, (await state.ReadAsync()).LastPromptUtc);
    }

    /// <summary>
    /// A state file written before the prompt existed has no such field. It must read as "never
    /// asked" rather than as unparseable — an upgrading device with no folder set is exactly who
    /// the question is for.
    /// </summary>
    [Fact]
    public async Task AFileFromBeforeThePromptExistedReadsAsNeverAsked()
    {
        await File.WriteAllTextAsync(
            path,
            """
            {
              "lastUploadUtc": null,
              "lastAttemptUtc": null,
              "destination": null
            }
            """);

        var read = await new CloudBackupState(path).ReadAsync();

        Assert.Null(read.LastPromptUtc);
        Assert.True(read.IsPromptDue(DateTime.Now));
    }

    [Fact]
    public async Task NullStampsSurviveTheRoundTrip()
    {
        // The state right after a folder is chosen: a destination, nothing uploaded yet.
        var state = new CloudBackupState(path);
        var written = new CloudBackupSchedule(null, null, "bookmark-bytes");

        await state.WriteAsync(written);
        var read = await state.ReadAsync();

        Assert.Null(read.LastUploadUtc);
        Assert.Null(read.LastAttemptUtc);
        Assert.Equal(written.Destination, read.Destination);
    }

    [Fact]
    public async Task GarbageReadsAsEmptyRatherThanThrowing()
    {
        // Losing this file must schedule a backup, never suppress one.
        await File.WriteAllTextAsync(path, "not json at all");

        Assert.Equal(CloudBackupSchedule.Empty, await new CloudBackupState(path).ReadAsync());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}