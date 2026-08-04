namespace DapperDemo.Viewmodel.Reports;

/// <summary>
/// Renders a <see cref="ReportDocument"/> to an image and asks the user where to keep it.
/// </summary>
/// <remarks>
/// An abstraction because the drawing and the save dialog both need the View layer. Not
/// I-prefixed, matching the framework's convention.
/// </remarks>
public interface ReportExporter
{
    /// <summary>
    /// Renders the report and saves it wherever the user chooses.
    /// </summary>
    /// <param name="report">The report's content.</param>
    /// <param name="suggestedFileName">The name to offer in the save dialog, without extension.</param>
    /// <returns>The name of the file written, or null if the user cancelled.</returns>
    Task<string?> ExportAsync(ReportDocument report, string suggestedFileName);
}