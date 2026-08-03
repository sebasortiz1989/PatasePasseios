using System.Collections.ObjectModel;

namespace DapperDemo.Viewmodel.Reports;

/// <summary>One table in a report: a heading, column titles, rows, and the totals beneath them.</summary>
public sealed class ReportSection
{
    /// <summary>Gets the heading above the table, e.g. a month name.</summary>
    public required string Heading { get; init; }

    /// <summary>Gets the column titles. Their count sets the table's width.</summary>
    public Collection<string> Columns { get; } = [];

    /// <summary>
    /// Gets the column alignments: false for left, true for right. Money and counts read better
    /// right-aligned. Missing entries fall back to left.
    /// </summary>
    public Collection<bool> RightAligned { get; } = [];

    /// <summary>
    /// Gets label/value lines printed under the heading, before any table. Used for blocks that
    /// are prose rather than a list, such as where to send a payment.
    /// </summary>
    public Collection<ReportField> Fields { get; } = [];

    public Collection<ReportRow> Rows { get; } = [];

    /// <summary>Gets the lines printed under the table, such as paid and pending subtotals.</summary>
    public Collection<ReportField> Totals { get; } = [];

    /// <summary>Gets or sets the line shown instead of the table when there are no rows.</summary>
    public string EmptyMessage { get; set; } = string.Empty;
}