using PatasePasseios.Repository.Dapper;

namespace PatasePasseios.Viewmodel.Services;

/// <summary>
/// Hands a file to whatever else the device can send it with — WhatsApp, e-mail, Drive.
/// </summary>
/// <remarks>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one, except that here the
/// platforms genuinely differ rather than merely needing a TopLevel: Android and iOS have a system
/// share sheet, desktop has no equivalent worth pretending about. <see cref="CanShare"/> is what a
/// screen asks before offering the button, so the desktop heads simply do not show it. Not
/// I-prefixed, matching the framework's convention.
/// </remarks>
public interface ShareSheet
{
    /// <summary>Gets a value indicating whether this device can hand a file to another app.</summary>
    bool CanShare { get; }

    /// <summary>
    /// Opens the system share sheet for a file already on disk.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to send.</param>
    /// <param name="title">The chooser's title, in pt-BR.</param>
    /// <returns>
    /// Successful once the sheet has been opened. It is not an answer about what the user did with
    /// it — the system hands the file to another app and never reports back, so there is nothing
    /// truthful to return beyond "the sheet opened".
    /// </returns>
    Task<Response> ShareFileAsync(string filePath, string title);
}