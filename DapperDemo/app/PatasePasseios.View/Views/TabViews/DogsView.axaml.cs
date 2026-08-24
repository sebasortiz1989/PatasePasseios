using Avalonia.Interactivity;
using AvaloniaFramework.Controls;
using PatasePasseios.Viewmodel.Viewmodels.Session;
using PatasePasseios.Viewmodel.Viewmodels.TabViewsViewmodels;

namespace PatasePasseios.View.Views.TabViews;

public partial class DogsView : PresenterUserControl<DogsViewModel, Unit, Unit>
{
    public DogsView()
    {
        InitializeComponent();
    }

    // The five tab views are handed to MainViewModel.CurrentView rather than pushed through the
    // NavigationController, so they are never RunAsync'd and OnRunStarting never fires for them.
    // OnLoaded is the reliable hook, and it runs again when the tab is shown after popping back
    // from a pushed screen — which is what keeps the list fresh after adding a record.
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AppSession.FireAndForget(PresentationModel.ReloadAsync());
    }
}