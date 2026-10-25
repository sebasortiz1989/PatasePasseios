using Avalonia;
using PatasePasseios.Desktop.DependencyInversion;
using PatasePasseios.View;
using System;
#if AVALONIA_MCP
using XamlMcp.Avalonia;
#endif

namespace PatasePasseios.Desktop;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure(() => new App(new DesktopContainerBuilder().Build()))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
#if AVALONIA_MCP
        // Inert unless XAML_MCP=1, which Claude Code's launch tool sets.
        builder = builder.AttachXamlMcp();
#endif
        return builder;
    }
}