using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

namespace DapperDemo.View.Views.NavigationViews;

public partial class SignUpView : PresenterUserControl<SignUpViewModel, Unit, Unit>
{
    public SignUpView()
    {
        InitializeComponent();
    }
}