using Avalonia.Interactivity;
using AvaloniaFramework.Controls;
using PatasePasseios.Viewmodel.Viewmodels;
using PatasePasseios.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using PatasePasseios.Viewmodel.Viewmodels.Session;

namespace PatasePasseios.View.Views.ComplementaryViews;

public partial class SettingsView : PresenterUserControl<SettingsViewModel, Unit, Unit>
{
    public SettingsView()
    {
        InitializeComponent();
    }

    // Shown by assigning CurrentView rather than pushed through the NavigationController, so the
    // presenter is never RunAsync'd and OnRunStarting never fires. OnLoaded runs on every reopen,
    // which is what seeds the controls from the stored preference.
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AppSession.FireAndForget(PresentationModel.ReloadAsync());
    }
}