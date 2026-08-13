using System.Collections.ObjectModel;

namespace DapperDemo.Viewmodel.Reports;

/// <summary>One line of a report table. Cells line up with the section's columns.</summary>
public sealed class ReportRow
{
    public ReportRow(params string[] cells)
    {
        Cells = [.. cells ?? []];
    }

    public Collection<string> Cells { get; }

    /// <summary>
    /// Gets the small print under a cell, by the same index as <see cref="Cells"/>. It is how a
    /// row says more than one line without every other row gaining a column it would leave empty:
    /// a hotel stay's check-out date sits under its check-in, and its daily rate under its total.
    /// Lines are separated by <c>\n</c>. Entries past the end, and empty ones, print nothing.
    /// </summary>
    public Collection<string> Details { get; } = [];

    /// <summary>
    /// Adds small print under one cell. Returns the row so a caller can chain, since the details
    /// are decided right where the row is built.
    /// </summary>
    /// <param name="column">Index of the cell the detail belongs under.</param>
    /// <param name="detail">The lines to print, separated by <c>\n</c>.</param>
    /// <returns>This row.</returns>
    public ReportRow WithDetail(int column, string detail)
    {
        while (Details.Count <= column)
        {
            Details.Add(string.Empty);
        }

        Details[column] = detail;
        return this;
    }

    /// <summary>Gets the small print under a cell, or an empty string when it has none.</summary>
    /// <param name="column">Index of the cell.</param>
    /// <returns>The detail lines, or an empty string.</returns>
    public string DetailAt(int column) => column < Details.Count ? Details[column] : string.Empty;
}