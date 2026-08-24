using AvaloniaFramework.Controls;
using PatasePasseios.Viewmodel.Viewmodels;
using PatasePasseios.Viewmodel.Viewmodels.NavigationViewsViewmodels;

namespace PatasePasseios.View.Views.NavigationViews;

public partial class SignUpView : PresenterUserControl<SignUpViewModel, Unit, Unit>
{
    public SignUpView()
    {
        InitializeComponent();
    }
}