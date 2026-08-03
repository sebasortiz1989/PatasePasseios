namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// Hands a URI to whatever application the platform associates with it — a browser, or the
/// Google Calendar app when it has registered the calendar.google.com links.
/// </summary>
/// <remarks>
/// An abstraction because launching needs a TopLevel, which only the View layer has, the same
/// reason <see cref="ImagePicker"/> is one. Not I-prefixed, matching the framework's convention.
/// </remarks>
public interface UriLauncher
{
    /// <summary>
    /// Opens the URI outside the app.
    /// </summary>
    /// <param name="uri">The absolute URI to hand over.</param>
    /// <returns>
    /// True when the operating system accepted the request. That is not a promise anything opened
    /// — only that nothing refused it.
    /// </returns>
    Task<bool> LaunchAsync(Uri uri);
}
