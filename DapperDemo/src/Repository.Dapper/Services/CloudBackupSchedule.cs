namespace DapperDemo.Repository.Dapper.Services;

/// <summary>
/// When this device last uploaded a backup, and when it last asked the user about doing so.
/// </summary>
/// <param name="LastUploadUtc">When an upload last succeeded, or null if one never has.</param>
/// <param name="LastPromptUtc">When the user was last asked, or null if they never have been.</param>
/// <param name="Destination">
/// The folder the user chose, as a storage bookmark, or null if they have not chosen one. Opaque
/// here on purpose: the data layer stores the string and the View layer is the only thing that
/// knows it is an Avalonia bookmark. On Android it also carries the SAF permission grant, which is
/// what lets a later launch write to that folder without asking again.
/// </param>
public sealed record CloudBackupSchedule(DateTime? LastUploadUtc, DateTime? LastPromptUtc, string? Destination = null)
{
    /// <summary>How long a backup stays fresh before another one is wanted.</summary>
    public static readonly TimeSpan UploadInterval = TimeSpan.FromDays(7);

    /// <summary>
    /// How long a declined prompt is respected for.
    /// </summary>
    /// <remarks>
    /// Shorter than <see cref="UploadInterval"/> on purpose. Deferring by a full week would mean
    /// one "Não" at a bad moment costs the user a whole cycle, and a habit of declining would stop
    /// backups happening at all rather than merely delaying them.
    /// </remarks>
    public static readonly TimeSpan RetryInterval = TimeSpan.FromDays(1);

    /// <summary>Gets the state of a device that has never uploaded and never asked.</summary>
    public static CloudBackupSchedule Empty { get; } = new(null, null, null);

    /// <summary>Gets a value indicating whether a destination has been chosen.</summary>
    public bool HasDestination => !string.IsNullOrWhiteSpace(Destination);

    /// <summary>
    /// Gets a value indicating whether the user should be asked to back up now.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>True when a backup is overdue and the last prompt is old enough to repeat.</returns>
    public bool IsDue(DateTime utcNow) =>
        Elapsed(LastUploadUtc, utcNow, UploadInterval) && Elapsed(LastPromptUtc, utcNow, RetryInterval);

    /// <summary>
    /// Whether <paramref name="stamp"/> is missing, older than <paramref name="interval"/>, or in
    /// the future.
    /// </summary>
    /// <remarks>
    /// A stamp ahead of the clock counts as elapsed rather than as "not yet". It happens when the
    /// device clock moves back or when the file arrives from a device that was running ahead, and
    /// treating it as "not yet" would stop backups until real time caught up — which for a clock
    /// set years forward is never.
    /// </remarks>
    private static bool Elapsed(DateTime? stamp, DateTime utcNow, TimeSpan interval) =>
        stamp is not { } value || value > utcNow || utcNow - value >= interval;
}