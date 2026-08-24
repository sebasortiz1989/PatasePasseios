using AppKit;
using Avalonia;
using PatasePasseios.MacOS.DependencyInversion;
using PatasePasseios.View;
using System;

namespace PatasePasseios.MacOS
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            NSApplication.Init();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure(() => new App(new MacContainerBuilder().Build()))
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}