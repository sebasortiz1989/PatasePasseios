using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

namespace DapperDemo.View.Views.NavigationViews;

public partial class MainView : PresenterUserControl<MainViewModel, Unit, Unit>
{
    public MainView()
    {
        InitializeComponent();
    }
}