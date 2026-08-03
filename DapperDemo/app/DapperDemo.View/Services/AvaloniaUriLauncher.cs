using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        var launcher = GetTopLevel()?.Launcher;
        if (launcher == null)
        {
            return false;
        }

        return await launcher.LaunchUriAsync(uri).ConfigureAwait(true);
    }

    /// <summary>
    /// The window (desktop) or root view (mobile) the launcher hangs off. Resolved from the
    /// lifetime rather than passed in, so view models can stay free of Avalonia types — the same
    /// approach <see cref="StorageProviderImagePicker"/> takes.
    /// </summary>
    private static TopLevel? GetTopLevel() => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
        _ => null,
    };
}
