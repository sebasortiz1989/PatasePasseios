using Avalonia.Interactivity;
using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.View.Views;

public partial class NewDogView : PresenterUserControl<NewDogViewModel, Unit, Unit>
{
    public NewDogView()
    {
        InitializeComponent();
    }

    // Shown by assigning MainViewModel.CurrentView rather than pushed through the
    // NavigationController, so the presenter is never RunAsync'd and OnRunStarting never fires.
    // OnLoaded runs on every reopen, which is what fills the tutor picker.
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AppSession.FireAndForget(PresentationModel.ReloadAsync());
    }
}