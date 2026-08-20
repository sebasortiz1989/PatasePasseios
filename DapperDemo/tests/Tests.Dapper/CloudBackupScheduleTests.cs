using DapperDemo.Repository.Dapper.Services;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// When the app decides a backup is overdue. No files — this is the pure rule that the prompt at
/// login and the "Enviar agora" button both sit on top of.
/// </summary>
public class CloudBackupScheduleTests
{
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ADeviceThatHasNeverBackedUpIsDue()
    {
        Assert.True(CloudBackupSchedule.Empty.IsDue(Now));
    }

    [Fact]
    public void AFreshBackupIsNotDue()
    {
        var schedule = new CloudBackupSchedule(Now.AddDays(-1), Now.AddDays(-1));

        Assert.False(schedule.IsDue(Now));
    }

    [Fact]
    public void ABackupOlderThanAWeekIsDue()
    {
        var schedule = new CloudBackupSchedule(Now.AddDays(-7), Now.AddDays(-7));

        Assert.True(schedule.IsDue(Now));
    }

    [Fact]
    public void DecliningHoldsTheQuestionForADay()
    {
        // Overdue backup, but the user said no an hour ago: asking again on the next launch is
        // what makes people stop reading the dialog.
        var schedule = new CloudBackupSchedule(Now.AddDays(-30), Now.AddHours(-1));

        Assert.False(schedule.IsDue(Now));
    }

    [Fact]
    public void TheQuestionComesBackTheNextDay()
    {
        var schedule = new CloudBackupSchedule(Now.AddDays(-30), Now.AddDays(-1));

        Assert.True(schedule.IsDue(Now));
    }

    [Fact]
    public void AStampFromTheFutureCountsAsDue()
    {
        // A clock that has moved back, or a state file from a device running ahead. Treating this
        // as "not yet" would suspend backups until real time caught up.
        var schedule = new CloudBackupSchedule(Now.AddYears(1), Now.AddYears(1));

        Assert.True(schedule.IsDue(Now));
    }
}