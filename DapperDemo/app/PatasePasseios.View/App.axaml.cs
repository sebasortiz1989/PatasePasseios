using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaFramework.DependencyInjection;
using AvaloniaFramework.Hosting;
using AvaloniaFramework.Hosting.Navigation;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using PatasePasseios.Repository.Dapper.Services;
using PatasePasseios.View.DependencyInversion;
using PatasePasseios.View.Services;
using PatasePasseios.Viewmodel.Services;
using PatasePasseios.Viewmodel.Viewmodels;
using PatasePasseios.Viewmodel.Viewmodels.NavigationViewsViewmodels;

namespace PatasePasseios.View;

public partial class App : ApplicationPreview
{
    public App(Container container)
        : base(container)
    {
    }

    public App()
        : base(new PatasePasseiosViewContainerBuilder().Build())
    {
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var navigationController = Container.Resolve<NavigationController>();

        // Before any window exists, so the first frame is already in the user's palette and type
        // size rather than flashing the default and correcting itself.
        //
        // Read, not ReadAsync: this method has to set the lifetime's MainWindow before it returns,
        // because Avalonia starts the main loop the moment it does. Awaiting anything above that
        // line hands control back with no window built and the app comes up blank — intermittently,
        // because a small cached file often reads without ever yielding.
        Container.Resolve<DisplaySettings>().Apply(Container.Resolve<DisplayPreferencesStore>().Read());

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