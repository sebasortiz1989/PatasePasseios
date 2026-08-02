using AvaloniaFramework.Controls;
using DapperDemo.Viewmodel.Viewmodels;

namespace DapperDemo.View.Views;

public partial class TutorDetailView : PresenterUserControl<TutorDetailViewModel, Unit, Unit>
{
    public TutorDetailView()
    {
        InitializeComponent();
    }
}
