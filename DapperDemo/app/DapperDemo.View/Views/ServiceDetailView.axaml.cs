using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class ServiceDetailView : PresenterUserControl<ServiceDetailViewModel, Unit, Unit>
{
    public ServiceDetailView()
    {
        InitializeComponent();
    }
}
