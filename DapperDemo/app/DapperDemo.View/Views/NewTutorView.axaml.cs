using Avalonia.Interactivity;
using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.View.Views;

public partial class NewTutorView : PresenterUserControl<NewTutorViewModel, Unit, Unit>
{
    public NewTutorView()
    {
        InitializeComponent();
    }

    // Shown by assigning MainViewModel.CurrentView rather than pushed through the
    // NavigationController, so the presenter is never RunAsync'd and OnRunStarting never fires.
    // OnLoaded runs on every reopen, which is what clears the form.
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AppSession.FireAndForget(PresentationModel.ReloadAsync());
    }
}