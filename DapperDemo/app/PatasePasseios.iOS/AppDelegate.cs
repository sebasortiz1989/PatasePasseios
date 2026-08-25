using Avalonia;
using Avalonia.iOS;
using Foundation;
using PatasePasseios.View;

namespace PatasePasseios.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the
// User Interface of the application, as well as listening (and optionally responding) to
// application events from iOS.
[Register("AppDelegate")]
internal sealed class AppDelegate : AvaloniaAppDelegate<AppIphone>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}