using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class DogRow(string initials, string name, string subtitle, ICommand openCommand)
{
    public string Initials { get; } = initials;

    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    public ICommand OpenCommand { get; } = openCommand;
}

[AddINotifyPropertyChangedInterface]
public class DogsViewModel : PresentationModelBase<Void, Void>
{
    private readonly NavigationController navigationController;
    private readonly MockAppData mockAppData;
    private readonly Factory<PresenterBase<DogDetailViewModel, Void, Void>> dogDetailFactory;

    public DogsViewModel(
        NavigationController navigationController,
        MockAppData mockAppData,
        Factory<PresenterBase<DogDetailViewModel, Void, Void>> dogDetailFactory)
    {
        this.navigationController = navigationController;
        this.mockAppData = mockAppData;
        this.dogDetailFactory = dogDetailFactory;
        Refresh();
    }

    public ObservableCollection<DogRow> DogsCollection { get; } = [];

    public string DogCountLabel { get; set; } = string.Empty;

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        DogsCollection.Clear();
        foreach (var dog in mockAppData.Dogs)
        {
            var owner = mockAppData.TutorById(dog.OwnerId);
            var openCommand = new SynchronizedCommand(() => Open(dog.Id), SynchronizationBehavior.Discard, true);
            DogsCollection.Add(new DogRow(MockAppData.Initials(dog.Name), dog.Name, $"{dog.Breed} · {owner?.Name}", openCommand));
        }

        DogCountLabel = $"{mockAppData.Dogs.Count} cadastrados";
    }

    private async Task Open(int dogId)
    {
        mockAppData.SelectedDogId = dogId;
        await navigationController.PushAsync(dogDetailFactory.Create()).WithSync();
    }
}
