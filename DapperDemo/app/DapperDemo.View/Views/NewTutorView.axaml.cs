using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class NewTutorView : PresenterUserControl<NewTutorViewModel, Unit, Unit>
{
    public NewTutorView()
    {
        InitializeComponent();
    }
}