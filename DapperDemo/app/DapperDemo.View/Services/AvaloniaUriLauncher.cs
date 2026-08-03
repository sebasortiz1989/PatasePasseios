using DapperDemo.Viewmodel.Services;
using System;
using System.Threading.Tasks;

namespace DapperDemo.View.Services;

/// <summary>
/// Launches URIs through Avalonia's launcher, which maps to the default browser on desktop and to
/// the system intent on Android and iOS. Supported on every head this app targets.
/// </summary>
public sealed class AvaloniaUriLauncher : UriLauncher
{
    /// <inheritdoc/>
    public async Task<bool> LaunchAsync(Uri uri)
    {
        var launcher = ShellTopLevel.Resolve()?.Launcher;
        if (launcher == null)
        {
            return false;
        }

        return await launcher.LaunchUriAsync(uri).ConfigureAwait(true);
    }
}