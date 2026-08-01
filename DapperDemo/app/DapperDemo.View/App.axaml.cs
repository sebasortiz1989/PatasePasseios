using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Verion.Apresentacao.Avalonia.Preview;
using Verion.Framework.Infraestrutura.DependencyInversion;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.View.Components;
using Verion.Treinamento.DapperDemo.View.DependencyInversion;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

namespace Verion.Treinamento.DapperDemo.View;

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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            // DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new WindowInicial();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new ViewInicial();
        }

        base.OnFrameworkInitializationCompleted();
        PreviewMobile.Container = Container;
        var navigationController = Container.Resolve<NavigationController>();
        var initialView = Container.Resolve<PresenterBase<LoginViewModel, Void, Void>>();
        await navigationController.PushAsync(initialView).WithSync();
    }
}