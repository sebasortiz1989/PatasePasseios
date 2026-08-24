namespace PatasePasseios.Viewmodel.Services;

/// <summary>
/// Opens the platform's file browser so the user can choose a photo.
/// </summary>
/// <remarks>
/// An abstraction because picking a file needs a TopLevel, which only the View layer has. The
/// view models depend on this interface; the Avalonia implementation is registered over it.
/// Not I-prefixed, matching the framework's convention.
/// </remarks>
public interface ImagePicker
{
    /// <summary>
    /// Asks the user to choose an image file.
    /// </summary>
    /// <returns>
    /// The chosen image, which the caller disposes, or null if the user cancelled or the
    /// platform offers no file browser.
    /// </returns>
    Task<PickedImage?> PickAsync();
}