using PatasePasseios.Repository.Dapper;
using PatasePasseios.Repository.Dapper.Services;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The palette and type-size preference. Read before anyone signs in — the login screen is drawn
/// in it — so a damaged file has to mean "looks ordinary", never "does not open".
/// </summary>
public sealed class DisplayPreferencesTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"dd-display-{Guid.NewGuid():N}.json");

    [Fact]
    public async Task AnUnconfiguredDeviceFollowsTheSystem()
    {
        var stored = await new DisplayPreferencesStore(path).ReadAsync();

        Assert.Equal(AppTheme.System, stored.Theme);
        Assert.True(stored.FollowSystemTextSize);
        Assert.Equal(DisplayPreferences.DefaultStep, stored.TextSizeStep);
    }

    [Theory]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.System)]
    public async Task AChoiceComesBack(AppTheme theme)
    {
        var store = new DisplayPreferencesStore(path);

        Assert.Equal(Response.Successful, await store.WriteAsync(new DisplayPreferences(theme, 5, false)));

        var read = await store.ReadAsync();

        Assert.Equal(theme, read.Theme);
        Assert.Equal(5, read.TextSizeStep);
        Assert.False(read.FollowSystemTextSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(7)]
    [InlineData(99)]
    public async Task AStepOutOfRangeIsClampedRatherThanHonoured(int step)
    {
        // Nothing stops a hand-edited file, and a step outside the six would index off the ramp.
        var store = new DisplayPreferencesStore(path);
        await store.WriteAsync(new DisplayPreferences(AppTheme.Light, step, false));

        var read = await store.ReadAsync();

        Assert.InRange(read.TextSizeStep, 1, DisplayPreferences.StepCount);
    }

    [Fact]
    public async Task GarbageReadsAsTheDefaultRatherThanThrowing()
    {
        await File.WriteAllTextAsync(path, "not json at all");

        Assert.Equal(DisplayPreferences.Default, await new DisplayPreferencesStore(path).ReadAsync());
    }

    [Fact]
    public async Task AFileMissingTheSizeKeysStillYieldsAUsablePreference()
    {
        // A file written by a build before the size control existed.
        await File.WriteAllTextAsync(path, """{ "theme": "Dark" }""");

        var read = await new DisplayPreferencesStore(path).ReadAsync();

        Assert.Equal(AppTheme.Dark, read.Theme);
        Assert.True(read.FollowSystemTextSize);
        Assert.InRange(read.TextSizeStep, 1, DisplayPreferences.StepCount);
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
