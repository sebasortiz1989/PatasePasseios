using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

public record FutureServiceRow(string TypeLabel, string DateLabel);

[AddINotifyPropertyChangedInterface]
public class DogDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;

    public DogDetailViewModel(
        CurrentView currentView,
        NavigationController navigationController,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        RepositoryServices repositoryServices,
        AppSession session)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryServices = repositoryServices;
        this.session = session;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>Deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    public bool NotConfirmingDelete => !ConfirmingDelete;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Breed { get; private set; } = string.Empty;

    public string OwnerName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool NoFuture { get; private set; }

    public ObservableCollection<FutureServiceRow> FutureServices { get; } = [];

    protected override async Task OnRunStarting(Unit input)
    {
        if (session.SelectedDogId is not int dogId)
        {
            return;
        }

        var dog = await repositoryDogs.GetAsync(dogId).WithSync();
        if (dog == null)
        {
            return;
        }

        Initials = AppSession.Initials(dog.Name);
        Name = dog.Name;
        Breed = dog.Breed ?? string.Empty;
        Description = string.IsNullOrWhiteSpace(dog.Description) ? "Sem descrição." : dog.Description;

        var tutor = await repositoryTutors.GetAsync(dog.TutorId).WithSync();
        OwnerName = tutor?.Name ?? string.Empty;

        var services = await repositoryServices.ListForDogAsync(session.CurrentPetSitterId, dogId).WithSync();
        var now = DateTime.Now;
        var future = services
            .Where(s => s.Date >= now)
            .OrderBy(s => s.Date)
            .Select(s => new FutureServiceRow(AppSession.TypeLabel(s.Kind), AppSession.DateTimeLabel(s.Date)))
            .ToArray();

        FutureServices.Clear();
        foreach (var row in future)
        {
            FutureServices.Add(row);
        }

        NoFuture = future.Length == 0;
    }

    private async Task Delete()
    {
        if (session.SelectedDogId is not int dogId)
        {
            return;
        }

        await repositoryDogs.Delete(dogId).WithSync();
        session.SelectedDogId = null;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }
}