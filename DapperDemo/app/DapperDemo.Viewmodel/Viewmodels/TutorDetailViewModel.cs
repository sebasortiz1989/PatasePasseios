using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
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

    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;

    public TutorDetailViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        RepositoryServices repositoryServices,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.session = session;
        this.currentView = currentView;
        serviceDetailView = serviceDetailFactory.Create();
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the screen is in edit mode — see
    /// <see cref="DogDetailViewModel.IsEditing"/> for why the editor replaces the fields in place
    /// rather than opening a separate form.
    /// </summary>
    public bool IsEditing { get; private set; }

    public bool IsViewing => !IsEditing;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Neighborhood { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string EditName { get; set; } = string.Empty;

    public string EditPhone { get; set; } = string.Empty;

    public string EditAddress { get; set; } = string.Empty;

    public string EditError { get; private set; } = string.Empty;

    public bool HasEditError => !string.IsNullOrEmpty(EditError);

    public string DogNames { get; private set; } = string.Empty;

    public bool NoFuture { get; private set; }

    public ObservableCollection<TutorFutureServiceRow> FutureServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded — see <see cref="DogDetailViewModel"/> for
    /// why OnRunStarting is not enough for a screen shown through CurrentView.
    /// </summary>
    public async Task ReloadAsync()
    {
        // Before the early returns below, so arriving here always lands on the reading state —
        // leaving mid-edit and coming back should not resume a half-finished form.
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;

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
            .ToArray();

        ClearFutureServices();
        foreach (var service in future)
        {
            // CA2000: ownership passes to the row, which disposes the command when the list is
            // rebuilt — see ClearFutureServices.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(
                () => Open(service.Kind, service.ServiceId),
                SynchronizationBehavior.Discard,
                true);
#pragma warning restore CA2000

            FutureServices.Add(new TutorFutureServiceRow(
                service.DogName,
                AppSession.TypeLabel(service.Kind),
                AppSession.DateTimeLabel(service.Date, service.Kind),
                service.ServicePaid,
                service.ServicePaid ? "Pago" : "Pendente",
                openCommand));
        }

        NoFuture = future.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        ClearFutureServices();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens the tapped service. CurrentView keeps a back stack, so the service screen's own Back
    /// returns here rather than to the tutors list.
    /// </summary>
    private Task Open(ServiceKind kind, int serviceId)
    {
        session.SelectedServiceKind = kind;
        session.SelectedServiceId = serviceId;
        currentView.ViewShown = serviceDetailView;
        return Task.CompletedTask;
    }

    private void ClearFutureServices()
    {
        foreach (var row in FutureServices)
        {
            row.Dispose();
        }

        FutureServices.Clear();
    }

    private Task StartEdit()
    {
        // Seeded from the loaded record, so cancelling and reopening the editor starts from the
        // saved values again rather than from whatever was typed and abandoned.
        EditName = Name;
        EditPhone = Phone;
        EditAddress = Neighborhood;
        EditError = string.Empty;
        IsEditing = true;
        return Task.CompletedTask;
    }

    private Task CancelEdit()
    {
        EditError = string.Empty;
        IsEditing = false;
        return Task.CompletedTask;
    }

    private async Task SaveEdit()
    {
        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            EditError = "Informe o nome do tutor.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditPhone))
        {
            EditError = "Informe o telefone.";
            return;
        }

        var result = await repositoryTutors.Update(new Tutors
        {
            TutorId = tutorId,
            Name = EditName.Trim(),
            Telephone = EditPhone.Trim(),
            Address = EditAddress.Trim(),
        }).WithSync();

        if (result != Response.Successful)
        {
            EditError = "Não foi possível salvar as alterações.";
            return;
        }

        IsEditing = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
    }

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