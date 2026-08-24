namespace PatasePasseios.Viewmodel.Reports;

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
    /// Renders the report to a temporary file and returns its path.
    /// </summary>
    /// <remarks>
    /// The image exists before the user is asked anything, which is what lets a screen show it and
    /// then offer to share or to save it — the same order a phone uses for a screenshot. The file
    /// lives in the temporary folder, which on Android is the app's cache: somewhere the share
    /// sheet can reach and the system can reclaim. The caller deletes it when the preview closes.
    /// </remarks>
    /// <param name="report">The report's content.</param>
    /// <param name="suggestedFileName">The name the file takes, without extension.</param>
    /// <returns>The absolute path of the rendered PNG, or null if it could not be written.</returns>
    Task<string?> RenderAsync(ReportDocument report, string suggestedFileName);

    /// <summary>
    /// Copies an already-rendered report to wherever the user chooses.
    /// </summary>
    /// <param name="renderedPath">A path returned by <see cref="RenderAsync"/>.</param>
    /// <param name="suggestedFileName">The name to offer in the save dialog, without extension.</param>
    /// <param name="confirmReplace">
    /// Asked with the file name when one is already there, on the platforms where the app does the
    /// naming itself. Returning false cancels the save.
    /// </param>
    /// <returns>The name of the file written, or null if the user cancelled.</returns>
    Task<string?> SaveAsync(string renderedPath, string suggestedFileName, Func<string, Task<bool>> confirmReplace);
}