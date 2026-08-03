using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using DapperDemo.Android.DependencyInversion;
using DapperDemo.View;
using System;

namespace DapperDemo.Android;

public class AppAndroid : App
{
    public AppAndroid()
        : base(new DroidContainerBuilder().Build())
    {
    }
}

/// <summary>
/// Android entry point. Avalonia 12 moved app initialization off <c>AvaloniaMainActivity</c> and onto
/// <see cref="AvaloniaAndroidApplication{TApp}"/>, so that activities created later in the process
/// lifetime reuse the same <see cref="AppAndroid"/> instance. Without this class nothing ever
/// constructs the Avalonia application and the activity dies with
/// "AvaloniaView initialization has failed".
/// </summary>
[global::Android.App.Application]
public sealed class DroidApplication : AvaloniaAndroidApplication<AppAndroid>
{
    public DroidApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    // Matches the Desktop/macOS heads' BuildAvaloniaApp and the iOS AppDelegate.
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
