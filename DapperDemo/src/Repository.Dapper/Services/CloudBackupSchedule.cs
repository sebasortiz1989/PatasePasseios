namespace DapperDemo.Repository.Dapper.Services;

/// <summary>
/// When this device last uploaded a backup, and where backups go.
/// </summary>
/// <param name="LastUploadUtc">When an upload last succeeded, or null if one never has.</param>
/// <param name="LastAttemptUtc">When one was last tried, whatever came of it, or null.</param>
/// <param name="Destination">
/// The folder the user chose, as a storage bookmark, or null if they have not chosen one. Opaque
/// here on purpose: the data layer stores the string and the View layer is the only thing that
/// knows it is an Avalonia bookmark. On Android it also carries the SAF permission grant, which is
/// what lets a later launch write to that folder without asking again.
/// </param>
public sealed record CloudBackupSchedule(DateTime? LastUploadUtc, DateTime? LastAttemptUtc = null, string? Destination = null)
{
    /// <summary>
    /// The local time of day the daily copy is taken at.
    /// </summary>
    /// <remarks>
    /// Local rather than UTC because "eight in the morning" is a statement about the sitter's
    /// clock. Early enough to be before the first walk of the day, so a copy exists before the
    /// day's records start changing.
    /// </remarks>
    public static readonly TimeSpan RunAt = TimeSpan.FromHours(8);

    /// <summary>
    /// How long a failed attempt is left alone before another is made.
    /// </summary>
    /// <remarks>
    /// The archive is the whole database plus every photo, and the schedule is checked every few
    /// minutes while the app is open — without a floor, a destination that is full or offline
    /// would have the device zipping all of it over and over for the rest of the day. An hour
    /// still gets the copy taken as soon as the trouble clears.
    /// </remarks>
    public static readonly TimeSpan RetryAfter = TimeSpan.FromHours(1);

    /// <summary>Gets the state of a device that has never uploaded and has no folder chosen.</summary>
    public static CloudBackupSchedule Empty { get; } = new(null, null, null);

    /// <summary>Gets a value indicating whether a destination has been chosen.</summary>
    public bool HasDestination => !string.IsNullOrWhiteSpace(Destination);

    /// <summary>
    /// The most recent moment a daily copy was due, in local time.
    /// </summary>
    /// <remarks>
    /// Today's <see cref="RunAt"/> once the clock has passed it, yesterday's before that. It is
    /// what makes the schedule a fixed time of day rather than a rolling twenty-four hours: an app
    /// opened at 08:05 backs up, and one opened again at 23:00 does not.
    /// </remarks>
    /// <param name="now">The device's local time.</param>
    /// <returns>The local moment of the last scheduled run.</returns>
    public static DateTime LastRunDue(DateTime now)
    {
        var today = now.Date + RunAt;
        return now >= today ? today : today.AddDays(-1);
    }

    /// <summary>
    /// Gets a value indicating whether the daily copy should be taken now.
    /// </summary>
    /// <remarks>
    /// A stamp ahead of the clock counts as due rather than as "not yet". It happens when the
    /// device clock moves back or when the file arrives from a device that was running ahead, and
    /// treating it as "not yet" would stop backups until real time caught up — which for a clock
    /// set years forward is never.
    /// </remarks>
    /// <param name="now">The device's local time.</param>
    /// <returns>True when no copy has been taken since the last scheduled run.</returns>
    public bool IsDue(DateTime now)
    {
        // A recent attempt holds the next one off whether or not it worked. On the day's first
        // check this is the same attempt that took the copy, so it changes nothing; it earns its
        // keep when the copy failed and the alternative is trying again every few minutes.
        if (Local(LastAttemptUtc) is { } attempted && attempted <= now && now - attempted < RetryAfter)
        {
            return false;
        }

        if (Local(LastUploadUtc) is not { } last)
        {
            return true;
        }

        return last > now || last < LastRunDue(now);
    }

    /// <summary>
    /// A stored stamp on the device's own clock.
    /// </summary>
    /// <remarks>
    /// The state file holds UTC, which is what the property names say; a value that arrives with
    /// any other kind is taken as already local rather than shifted by an unknown amount.
    /// </remarks>
    private static DateTime? Local(DateTime? stamp) => stamp switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } value => value.ToLocalTime(),
        { } value => value,
    };
}
