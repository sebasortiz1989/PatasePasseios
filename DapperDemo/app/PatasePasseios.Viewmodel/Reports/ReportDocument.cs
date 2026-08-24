using System.Collections.ObjectModel;

namespace PatasePasseios.Viewmodel.Reports;

/// <summary>
/// A report as content rather than as an image: headings, tables and totals, with nothing about
/// fonts, pixels or page size.
/// </summary>
/// <remarks>
/// The view models build one of these and hand it to a <see cref="ReportExporter"/>. Keeping the
/// shape here means the drawing stays entirely in the View layer, and the view models never
/// reference Avalonia.
/// </remarks>
public sealed class ReportDocument
{
    /// <summary>Gets the report's main heading, e.g. "Faturamento".</summary>
    public required string Title { get; init; }

    /// <summary>Gets the line under the title, e.g. "Agosto de 2026".</summary>
    public required string Subtitle { get; init; }

    /// <summary>Gets label/value pairs printed above the tables — who the report is about.</summary>
    public Collection<ReportField> Summary { get; } = [];

    /// <summary>Gets the tables, in order.</summary>
    public Collection<ReportSection> Sections { get; } = [];

    /// <summary>Gets or sets the small print at the foot.</summary>
    public string Footer { get; set; } = string.Empty;
}