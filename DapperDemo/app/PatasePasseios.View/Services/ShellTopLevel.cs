using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PatasePasseios.View.Services;

/// <summary>
/// Finds the <see cref="TopLevel"/> that platform services — file pickers, the launcher — hang
/// off, whichever lifetime the running head uses.
/// </summary>
/// <remarks>
/// Centralised because Android needs a different answer from everything else. Its
/// <see cref="IActivityApplicationLifetime"/> exposes no MainView, so there is nothing to walk up
/// from; the shell has to be remembered as it is created. Each service resolving this for itself
/// meant every one of them silently returned null on Android and did nothing.
/// </remarks>
internal static class ShellTopLevel
{
    /// <summary>
    /// Gets or sets the shell an activity-based platform last created. Assigned by
    /// <c>App.OnFrameworkInitializationCompleted</c> from the lifetime's view factory.
    /// </summary>
    public static Control? Current { get; set; }

    /// <summary>The current top level, or null before the shell exists.</summary>
    public static TopLevel? Resolve() => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,

        // Ahead of the single-view case: an implementation may satisfy both, and on Android only
        // this branch has anything to return.
        IActivityApplicationLifetime => Current is null ? null : TopLevel.GetTopLevel(Current),

        ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),

        _ => null,
    };
}