using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class NewDogView : PresenterUserControl<NewDogViewModel, Unit, Unit>
{
    public NewDogView()
    {
        InitializeComponent();
    }
}