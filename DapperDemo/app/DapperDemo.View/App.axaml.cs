using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaFramework.DependencyInjection;
using AvaloniaFramework.Hosting;
using AvaloniaFramework.Hosting.Navigation;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.View.DependencyInversion;
using DapperDemo.View.Services;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

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
        var navigationController = Container.Resolve<NavigationController>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ShellWindow();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
        {
            // Android, from Avalonia 12 on. The lifetime asks for a factory rather than an
            // instance because it recreates the activity — on rotation, or after the process is
            // reclaimed — so each new shell has to be handed to the controller, which owns the
            // navigation stack and puts the current screen back into it.
            activityLifetime.MainViewFactory = () =>
            {
                var shell = new ShellView();
                (navigationController as AvaloniaNavigationController)?.AttachShell(shell);
                ShellTopLevel.Current = shell;
                return shell;
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new ShellView();
        }

        base.OnFrameworkInitializationCompleted();
        var initialView = Container.Resolve<PresenterBase<LoginViewModel, Unit, Unit>>();
        await navigationController.PushAsync(initialView).WithSync();
    }
}