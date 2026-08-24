namespace PatasePasseios.Repository.Dapper.Services;

/// <summary>Which palette the app draws in.</summary>
public enum AppTheme
{
    /// <summary>Whatever the operating system is set to, and changing with it.</summary>
    System,

    /// <summary>Always the light palette.</summary>
    Light,

    /// <summary>Always the dark palette.</summary>
    Dark,
}

/// <summary>
/// How the app looks, as the user set it: the palette, and the size of the type.
/// </summary>
/// <param name="Theme">Which palette to draw in.</param>
/// <param name="TextSizeStep">Which of the six steps the type ramp sits on, 1 to 6.</param>
/// <param name="FollowSystemTextSize">
/// Whether the operating system's text size drives the ramp instead of
/// <paramref name="TextSizeStep"/>. When true the in-app control is shown but inert — it is a
/// readout of where the system has put the ramp, not a dependent control.
/// </param>
public sealed record DisplayPreferences(AppTheme Theme, int TextSizeStep, bool FollowSystemTextSize)
{
    /// <summary>The step an unconfigured app sits on — "Padrão".</summary>
    public const int DefaultStep = 2;

    /// <summary>The number of steps the ramp offers.</summary>
    public const int StepCount = 6;

    /// <summary>
    /// Gets what a device that has never opened Ajustes uses: the system's palette, the system's size.
    /// </summary>
    public static DisplayPreferences Default { get; } = new(AppTheme.System, DefaultStep, true);

    /// <summary>Gets this preference with the step forced into range.</summary>
    /// <returns>A record whose step is between 1 and <see cref="StepCount"/>.</returns>
    public DisplayPreferences Clamped() => TextSizeStep >= 1 && TextSizeStep <= StepCount
        ? this
        : this with { TextSizeStep = Math.Clamp(TextSizeStep, 1, StepCount) };
}