using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class MainView : PresenterUserControl<MainViewModel, Unit, Unit>
{
    public MainView()
    {
        InitializeComponent();
    }
}