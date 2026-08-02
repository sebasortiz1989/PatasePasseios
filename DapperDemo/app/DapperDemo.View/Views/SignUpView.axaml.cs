using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class SignUpView : PresenterUserControl<SignUpViewModel, Unit, Unit>
{
    public SignUpView()
    {
        InitializeComponent();
    }
}