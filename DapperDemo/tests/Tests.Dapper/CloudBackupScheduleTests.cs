using PatasePasseios.Repository.Dapper.Services;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// When the app decides today's copy is still owed. No files — this is the pure rule the daily run
/// and the "Enviar agora" button both sit on top of.
/// </summary>
/// <remarks>
/// Stamps are built with <see cref="DateTimeKind.Local"/> so the arithmetic under test is the rule
/// itself rather than whatever zone the machine running the tests is in. The one case that does
/// exercise the UTC conversion says so.
/// </remarks>
public class CloudBackupScheduleTests
{
    /// <summary>A Wednesday, well after the eight o'clock run.</summary>
    private static readonly DateTime Midday = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Local);

    /// <summary>The same Wednesday, before it.</summary>
    private static readonly DateTime EarlyMorning = new(2026, 8, 19, 6, 0, 0, DateTimeKind.Local);

    [Fact]
    public void ADeviceThatHasNeverBackedUpIsDue()
    {
        Assert.True(CloudBackupSchedule.Empty.IsDue(Midday));
    }

    [Fact]
    public void ACopyTakenSinceEightThisMorningIsEnough()
    {
        var schedule = new CloudBackupSchedule(LastUploadUtc: new DateTime(2026, 8, 19, 8, 5, 0, DateTimeKind.Local));

        Assert.False(schedule.IsDue(Midday));
    }

    [Fact]
    public void YesterdaysCopyIsDueAgainOnceEightHasPassed()
    {
        var schedule = new CloudBackupSchedule(LastUploadUtc: new DateTime(2026, 8, 18, 8, 5, 0, DateTimeKind.Local));

        Assert.True(schedule.IsDue(Midday));
    }

    [Fact]
    public void BeforeEightYesterdaysCopyStillCounts()
    {
        // The schedule is a time of day, not a rolling twenty-four hours: an app opened at six in
        // the morning has not reached today's run yet.
        var schedule = new CloudBackupSchedule(LastUploadUtc: new DateTime(2026, 8, 18, 8, 5, 0, DateTimeKind.Local));

        Assert.False(schedule.IsDue(EarlyMorning));
    }

    [Fact]
    public void BeforeEightACopyFromTheDayBeforeIsDue()
    {
        var schedule = new CloudBackupSchedule(LastUploadUtc: new DateTime(2026, 8, 17, 8, 5, 0, DateTimeKind.Local));

        Assert.True(schedule.IsDue(EarlyMorning));
    }

    [Fact]
    public void ACopyTakenLateInTheEveningStillCoversTheNextMorning()
    {
        // Taken at 22:00 on the 18th, which is after that day's run. At 06:00 on the 19th the last
        // run due is still the 18th's, so nothing is owed until eight.
        var schedule = new CloudBackupSchedule(LastUploadUtc: new DateTime(2026, 8, 18, 22, 0, 0, DateTimeKind.Local));

        Assert.False(schedule.IsDue(EarlyMorning));
        Assert.True(schedule.IsDue(Midday));
    }

    [Fact]
    public void AStampFromTheFutureCountsAsDue()
    {
        // A clock that has moved back, or a state file from a device running ahead. Treating this
        // as "not yet" would suspend backups until real time caught up.
        var schedule = new CloudBackupSchedule(LastUploadUtc: Midday.AddYears(1));

        Assert.True(schedule.IsDue(Midday));
    }

    [Fact]
    public void TheStoredStampIsReadAsUtc()
    {
        // What the state file actually holds is UTC, so the rule has to convert before comparing.
        // Anchored on the real clock rather than a fixed date, which keeps it true in every zone:
        // a copy taken this instant can never predate the last scheduled run.
        var schedule = new CloudBackupSchedule(LastUploadUtc: DateTime.UtcNow);

        Assert.False(schedule.IsDue(DateTime.Now));
    }

    [Fact]
    public void TheLastRunDueIsTodaysEightOrYesterdays()
    {
        Assert.Equal(new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Local), CloudBackupSchedule.LastRunDue(Midday));
        Assert.Equal(new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Local), CloudBackupSchedule.LastRunDue(EarlyMorning));
    }

    [Fact]
    public void AFailedAttemptIsNotRetriedStraightAway()
    {
        // The archive is the whole database and every photo. A destination that is full or
        // offline must not have the device rebuilding it at every check for the rest of the day.
        var schedule = new CloudBackupSchedule(
            LastUploadUtc: new DateTime(2026, 8, 17, 8, 5, 0, DateTimeKind.Local),
            LastAttemptUtc: Midday.AddMinutes(-10));

        Assert.False(schedule.IsDue(Midday));
    }

    [Fact]
    public void AFailedAttemptIsRetriedOnceItHasAged()
    {
        var schedule = new CloudBackupSchedule(
            LastUploadUtc: new DateTime(2026, 8, 17, 8, 5, 0, DateTimeKind.Local),
            LastAttemptUtc: Midday.AddHours(-2));

        Assert.True(schedule.IsDue(Midday));
    }

    // ---- Asking for a folder: the rule behind the once-a-day dialog and the Perfil banner. ----

    [Fact]
    public void ADeviceWithNoFolderIsAskedOnTheFirstSignIn()
    {
        Assert.True(CloudBackupSchedule.Empty.IsPromptDue(Midday));
    }

    [Fact]
    public void AskingTwiceOnTheSameDayDoesNotHappen()
    {
        var schedule = CloudBackupSchedule.Empty with { LastPromptUtc = EarlyMorning };

        Assert.False(schedule.IsPromptDue(Midday));
    }

    [Fact]
    public void TheQuestionComesBackTheNextDay()
    {
        var schedule = CloudBackupSchedule.Empty with
        {
            LastPromptUtc = new DateTime(2026, 8, 18, 23, 30, 0, DateTimeKind.Local),
        };

        Assert.True(schedule.IsPromptDue(EarlyMorning));
    }

    /// <summary>
    /// A calendar day, not a rolling twenty-four hours: asked late last night, asked again this
    /// morning. Deliberate — the alternative drifts the question later every day.
    /// </summary>
    [Fact]
    public void TheDayRollsOverAtMidnightRatherThanAfterTwentyFourHours()
    {
        var lastNight = new DateTime(2026, 8, 18, 23, 0, 0, DateTimeKind.Local);
        var schedule = CloudBackupSchedule.Empty with { LastPromptUtc = lastNight };

        Assert.True(schedule.IsPromptDue(lastNight.AddHours(2)));
    }

    /// <summary>
    /// The whole point of the feature: choosing a folder ends the asking permanently, whatever the
    /// stamp says and whether or not a copy has ever been taken.
    /// </summary>
    [Fact]
    public void ChoosingAFolderStopsTheAskingForGood()
    {
        var schedule = new CloudBackupSchedule(null, null, "bookmark://backups");

        Assert.False(schedule.IsPromptDue(Midday));
        Assert.False(schedule.IsPromptDue(Midday.AddYears(1)));
    }

    /// <summary>
    /// A folder that has since been deleted or had its permission revoked must not reopen the
    /// dialog. The sitter answered this question; an unreachable folder is reported in Perfil.
    /// </summary>
    [Fact]
    public void AnUnreachableFolderIsStillAnAnsweredQuestion()
    {
        var schedule = new CloudBackupSchedule(null, null, "bookmark://gone", LastPromptUtc: null);

        Assert.False(schedule.IsPromptDue(Midday));
    }

    /// <summary>
    /// A stamp ahead of the clock counts as due, matching <c>IsDue</c>: a device whose clock jumped
    /// forward and back must not be left permanently unasked.
    /// </summary>
    [Fact]
    public void AStampFromTheFutureDoesNotSilenceTheQuestion()
    {
        var schedule = CloudBackupSchedule.Empty with { LastPromptUtc = Midday.AddYears(1) };

        Assert.True(schedule.IsPromptDue(Midday));
    }
}
