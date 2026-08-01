using PropertyChanged;
using Verion.Presentation.View.UseCase;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

[AddINotifyPropertyChangedInterface]
public class ServicesViewModel : PresentationModelBase<Void, Void>
{
    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }
}