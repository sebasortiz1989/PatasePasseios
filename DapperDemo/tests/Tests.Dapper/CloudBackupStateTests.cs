using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Services;
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
        "dapperdemo-state-tests-" + Guid.NewGuid().ToString("N") + ".json");

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
            new DateTime(2026, 8, 18, 7, 0, 0, DateTimeKind.Utc));

        Assert.Equal(Response.Successful, await state.WriteAsync(written));

        var read = await state.ReadAsync();

        Assert.Equal(written.LastUploadUtc, read.LastUploadUtc);
        Assert.Equal(written.LastPromptUtc, read.LastPromptUtc);
    }

    [Fact]
    public async Task NullStampsSurviveTheRoundTrip()
    {
        // The state after a first prompt that was declined: asked, never uploaded.
        var state = new CloudBackupState(path);
        var written = new CloudBackupSchedule(null, new DateTime(2026, 8, 18, 7, 0, 0, DateTimeKind.Utc));

        await state.WriteAsync(written);
        var read = await state.ReadAsync();

        Assert.Null(read.LastUploadUtc);
        Assert.Equal(written.LastPromptUtc, read.LastPromptUtc);
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