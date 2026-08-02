using Avalonia.Interactivity;
using Verion.Apresentacao.Avalonia.Preview;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Session;

namespace Verion.Treinamento.DapperDemo.View.Views.MainViewViews;

public partial class UsersView : UserControl<UsersViewModel, Void, Void>
{
    public UsersView()
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
        AppSession.FireAndForget(ModeloApresentacao.ReloadAsync());
    }
}
