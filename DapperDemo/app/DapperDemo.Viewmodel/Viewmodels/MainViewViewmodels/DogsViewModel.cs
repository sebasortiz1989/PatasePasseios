using System.Collections.ObjectModel;
using PropertyChanged;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Dtos;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

[AddINotifyPropertyChangedInterface]
public class DogsViewModel : PresentationModelBase<Void, Void>
{
    private readonly SynchronizationContext synchronizationContext;
    private readonly RepositoryDogs repositoryDogs;

    public DogsViewModel(SynchronizationContext synchronizationContext, RepositoryDogs repositoryDogs)
    {
        this.synchronizationContext = synchronizationContext;
        this.repositoryDogs = repositoryDogs;
    }

    public ObservableCollection<Dogs> DogsCollection { get; set; } = new();

    public void InitializeDogs()
    {
        repositoryDogs.GetAll(dogsArray =>
        {
            synchronizationContext.SwitchTo();
            DogsCollection.Clear();
            foreach (var dog in dogsArray)
            {
                DogsCollection.Add(dog);
            }
        });
    }

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }
}