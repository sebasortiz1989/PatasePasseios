using System.Collections.ObjectModel;

namespace PatasePasseios.Viewmodel.Reports;

/// <summary>One block of a report: a heading, optional label/value lines, a table, and totals.</summary>
public sealed class ReportSection
{
    /// <summary>Gets the heading above the block, e.g. a month name.</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// Gets label/value lines printed under the heading, before any table. Used for blocks that
    /// are prose rather than a list, such as where to send a payment.
    /// </summary>
    public Collection<ReportField> Fields { get; } = [];

    /// <summary>Gets the column titles. Their count sets the table's width.</summary>
    public Collection<string> Columns { get; } = [];

    /// <summary>
    /// Gets the column alignments: false for left, true for right. Money and statuses read better
    /// right-aligned. Missing entries fall back to left.
    /// </summary>
    public Collection<bool> RightAligned { get; } = [];

    public Collection<ReportRow> Rows { get; } = [];

    /// <summary>Gets the lines under the table, such as paid and pending subtotals.</summary>
    public Collection<ReportField> Totals { get; } = [];

    /// <summary>Gets or sets the line shown instead of the table when there are no rows.</summary>
    public string EmptyMessage { get; set; } = string.Empty;
}