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
}