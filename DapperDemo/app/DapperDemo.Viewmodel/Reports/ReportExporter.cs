namespace DapperDemo.Viewmodel.Reports;

/// <summary>
/// Turns a <see cref="ReportDocument"/> into a PDF and asks the user where to keep it.
/// </summary>
/// <remarks>
/// An abstraction for the same reason ImagePicker is one: rendering and the save dialog both
/// need the View layer, and the view models should not depend on a PDF library. Not I-prefixed,
/// matching the framework's convention.
/// </remarks>
public interface ReportExporter
{
    /// <summary>
    /// Renders the report and saves it wherever the user chooses.
    /// </summary>
    /// <param name="report">The report's content.</param>
    /// <param name="suggestedFileName">The name to offer in the save dialog, without extension.</param>
    /// <returns>
    /// The name of the file written, or null if the user cancelled or the platform offers no
    /// save dialog.
    /// </returns>
    Task<string?> ExportAsync(ReportDocument report, string suggestedFileName);
}