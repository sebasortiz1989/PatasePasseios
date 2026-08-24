namespace PatasePasseios.Viewmodel.Reports;

/// <summary>A labelled value, used for report headers and for the totals under a table.</summary>
/// <param name="label">What the figure is.</param>
/// <param name="value">The figure, already formatted.</param>
/// <param name="emphasised">Whether it is the one to draw the eye, such as an amount due.</param>
public sealed class ReportField(string label, string value, bool emphasised = false)
{
    public string Label { get; } = label;

    public string Value { get; } = value;

    /// <summary>Gets a value indicating whether this line is drawn bold and in the accent colour.</summary>
    public bool Emphasised { get; } = emphasised;
}