using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly CurrentView currentView;

    public TutorDetailViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        RepositoryServices repositoryServices,
        AppSession session)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.session = session;
        this.currentView = currentView;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    public bool NotConfirmingDelete => !ConfirmingDelete;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Neighborhood { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string DogNames { get; private set; } = string.Empty;

    public bool NoFuture { get; private set; }

    public ObservableCollection<TutorFutureServiceRow> FutureServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded — see <see cref="DogDetailViewModel"/> for
    /// why OnRunStarting is not enough for a screen shown through CurrentView.
    /// </summary>
    public async Task ReloadAsync()
    {
        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        var tutor = await repositoryTutors.GetAsync(tutorId).WithSync();
        if (tutor == null)
        {
            return;
        }

        Initials = AppSession.Initials(tutor.Name);
        Name = tutor.Name;
        Neighborhood = tutor.Address ?? string.Empty;
        Phone = tutor.Telephone;

        var dogs = await repositoryDogs.ListForTutorAsync(tutorId).WithSync();
        DogNames = dogs.Length == 0 ? "Nenhum cachorro cadastrado." : string.Join(", ", dogs.Select(d => d.Name));

        var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogIds = dogs.Select(d => d.DogId).ToHashSet();
        var now = DateTime.Now;
        var future = services
            .Where(s => dogIds.Contains(s.DogId) && s.Date >= now)
            .OrderBy(s => s.Date)
            .Select(s => new TutorFutureServiceRow(s.DogName, AppSession.TypeLabel(s.Kind), AppSession.DateTimeLabel(s.Date)))
            .ToArray();

        FutureServices.Clear();
        foreach (var row in future)
        {
            FutureServices.Add(row);
        }

        NoFuture = future.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    private async Task Delete()
    {
        if (session.SelectedTutorId is not { } tutorId)
        {
            return;
        }

        // Cascades to this tutor's dogs and their services — see RepositoryTutors.Delete.
        await repositoryTutors.Delete(tutorId).WithSync();
        session.SelectedTutorId = null;
        session.NotifyDataChanged();
        currentView.GoBack();
    }
}