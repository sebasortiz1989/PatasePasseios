using DapperDemo.Repository.Dapper.Services;

namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// The six sizes the app offers, and what each one does to the seven type roles.
/// </summary>
/// <remarks>
/// <para>
/// The control sets <see cref="TextSizeStep.Body"/> and every other role follows a fixed ratio, so
/// the ramp cannot drift out of proportion as it grows. The numbers are the design's px scaled to
/// this app's 720-wide canvas — its px times 1.7476, times a further 1.1 because text and the
/// controls sized around it carry that second factor in this repo.
/// </para>
/// <para>
/// Margins, hit targets, the tab bar, group radius and row padding are deliberately absent: they
/// do not scale. The measure narrows as the type grows, which is what large type is for.
/// </para>
/// </remarks>
public static class TextSizeRamp
{
    /// <summary>Gets the six steps, smallest first.</summary>
    public static IReadOnlyList<TextSizeStep> Steps { get; } =
    [
        new(1, "Pequeno", 73, 50, 38, 31, 27, 23, 19),
        new(2, "Padrão", 81, 58, 42, 35, 29, 25, 21),
        new(3, "Grande", 90, 63, 46, 38, 33, 29, 23),
        new(4, "Maior", 104, 73, 54, 44, 38, 33, 27),
        new(5, "Muito maior", 117, 83, 61, 50, 42, 37, 31),
        new(6, "Máximo", 131, 92, 67, 56, 48, 40, 35),
    ];

    /// <summary>Gets one step by its number, clamped into range.</summary>
    /// <param name="number">The step, 1 to 6. Anything outside is clamped rather than rejected.</param>
    /// <returns>The step.</returns>
    public static TextSizeStep At(int number) =>
        Steps[Math.Clamp(number, 1, DisplayPreferences.StepCount) - 1];
}