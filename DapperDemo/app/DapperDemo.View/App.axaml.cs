using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaFramework.DependencyInjection;
using AvaloniaFramework.Hosting;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.View.DependencyInversion;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View;

public partial class App : ApplicationPreview
{
    public App(Container container)
        : base(container)
    {
    }

    public App()
        : base(new DapperDemoViewContainerBuilder().Build())
    {
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ShellWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new ShellView();
        }

        base.OnFrameworkInitializationCompleted();

        var navigationController = Container.Resolve<NavigationController>();
        var initialView = Container.Resolve<PresenterBase<LoginViewModel, Unit, Unit>>();
        await navigationController.PushAsync(initialView).WithSync();
    }
}
