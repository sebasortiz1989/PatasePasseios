using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class DogDetailView : PresenterUserControl<DogDetailViewModel, Unit, Unit>
{
    public DogDetailView()
    {
        InitializeComponent();
    }
}
