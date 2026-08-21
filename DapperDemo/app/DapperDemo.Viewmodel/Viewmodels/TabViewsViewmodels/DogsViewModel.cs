using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class DogsViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;
    private readonly PresenterBase<DogDetailViewModel, Unit, Unit> dogDetailView;
    private readonly PresenterBase<NewDogViewModel, Unit, Unit> newDogView;
    private readonly CurrentView currentView;

    public DogsViewModel(
        CurrentView currentView,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        AppSession session,
        Factory<PresenterBase<DogDetailViewModel, Unit, Unit>> dogDetailFactory,
        Factory<PresenterBase<NewDogViewModel, Unit, Unit>> newDogFactory)
    {
        this.currentView = currentView;
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.session = session;

        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        dogDetailView = dogDetailFactory.Create();
        newDogView = newDogFactory.Create();
        AddDogCommand = new SynchronizedCommand(() => currentView.Show(newDogView), SynchronizationBehavior.Discard, true);
    }

    public ICommand AddDogCommand { get; }

    public ObservableCollection<DogRow> DogsCollection { get; } = [];

    public string DogCountLabel { get; private set; } = string.Empty;

    // Defaults to true so the screen shows its empty-state message rather than nothing at all
    // in the moment before the first load completes.
    public bool IsEmpty { get; private set; } = true;

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var tutorNames = tutors.ToDictionary(t => t.TutorId, t => t.Name);

        ClearRows();
        foreach (var dog in dogs)
        {
            var ownerName = tutorNames.TryGetValue(dog.TutorId, out var name) ? name : string.Empty;
            var subtitle = string.IsNullOrWhiteSpace(dog.Breed) ? ownerName : $"{dog.Breed} · {ownerName}";

            // CA2000: ownership passes to the DogRow, which disposes the command when the list rebuilds.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(() => Open(dog.DogId), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
            DogsCollection.Add(new DogRow(AppSession.Initials(dog.Name), dog.Name, subtitle, DogImageStore.ResolvePath(dog.Image), openCommand));
        }

        DogCountLabel = dogs.Length == 1 ? "1 cadastrado" : $"{dogs.Length} cadastrados";
        IsEmpty = dogs.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        ClearRows();
        return Task.CompletedTask;
    }

    private void ClearRows()
    {
        foreach (var row in DogsCollection)
        {
            row.Dispose();
        }

        DogsCollection.Clear();
    }

    private Task Open(int dogId)
    {
        session.SelectedDogId = dogId;
        currentView.Show(dogDetailView);
        return Task.CompletedTask;
    }
}