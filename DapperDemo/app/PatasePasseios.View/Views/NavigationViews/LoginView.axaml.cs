using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaFramework.Controls;
using PatasePasseios.Viewmodel.Viewmodels;
using PatasePasseios.Viewmodel.Viewmodels.NavigationViewsViewmodels;

namespace PatasePasseios.View.Views.NavigationViews;

public partial class LoginView : PresenterUserControl<LoginViewModel, Unit, Unit>
{
    public LoginView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var insetsManager = TopLevel.GetTopLevel(this);

        if (insetsManager?.InsetsManager != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            insetsManager.InsetsManager.IsSystemBarVisible = false;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}