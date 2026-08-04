using System.Collections.ObjectModel;
using System.Windows.Input;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class DogDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryServices repositoryServices;
    private readonly ImagePicker imagePicker;
    private readonly AppSession session;
    private readonly CurrentView currentView;
    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;

    /// <summary>
    /// The photo file name currently in the database, as opposed to <see cref="PhotoFileName"/>
    /// which is what the open editor would save. The two differ while a new photo has been picked
    /// but not yet saved, which is what lets Cancel put the old one back.
    /// </summary>
    private string storedPhotoFileName = string.Empty;

    /// <summary>The tutor the record was loaded with, so reopening the editor after a cancelled
    /// edit starts from the saved owner rather than the one that was picked and abandoned.</summary>
    private int storedTutorId;

    /// <summary>
    /// Guards the picker hooks while <see cref="ReloadAsync"/> rebuilds the year list. Assigning
    /// SelectedYear there would otherwise re-enter the reload through Fody's OnXChanged hook.
    /// </summary>
    private bool rebuildingOptions;

    public DogDetailViewModel(
        CurrentView currentView,
        NavigationController navigationController,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        RepositoryServices repositoryServices,
        ImagePicker imagePicker,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryServices = repositoryServices;
        this.imagePicker = imagePicker;
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
        ChoosePhotoCommand = new SynchronizedCommand(ChoosePhoto, SynchronizationBehavior.Discard, true);
        RemovePhotoCommand = new SynchronizedCommand(RemovePhoto, SynchronizationBehavior.Discard, true);

        foreach (var month in ServicePeriod.Months())
        {
            MonthOptions.Add(month);
        }

        // "Ano todo" by default so opening a dog still shows everything booked this year, rather
        // than silently hiding next month's bookings behind a month picker.
        SelectedMonth = MonthOptions[0];
        SelectedYear = DateTime.Now.Year;
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the screen is in edit mode. The same fields are shown
    /// either way — as text while reading, as inputs while editing — so the record stays in front
    /// of the user rather than being replaced by a separate form.
    /// </summary>
    public bool IsEditing { get; private set; }

    public bool IsViewing => !IsEditing;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Breed { get; private set; } = string.Empty;

    public string OwnerName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the photo file name the editor would save; also what the read view shows.</summary>
    public string PhotoFileName { get; private set; } = string.Empty;

    /// <summary>Gets the photo's full path, or null when the dog has none.</summary>
    public string? PhotoPath => DogImageStore.ResolvePath(PhotoFileName);

    public bool HasPhoto => PhotoPath != null;

    public bool NoPhoto => !HasPhoto;

    public string EditName { get; set; } = string.Empty;

    public string EditBreed { get; set; } = string.Empty;

    public string EditDescription { get; set; } = string.Empty;

    public TutorOption? EditTutor { get; set; }

    /// <summary>Gets the tutors this dog may be assigned to, loaded alongside the record.</summary>
    public ObservableCollection<TutorOption> TutorOptions { get; } = [];

    public string EditError { get; private set; } = string.Empty;

    public bool HasEditError => !string.IsNullOrEmpty(EditError);

    public bool NoFuture { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether settled bookings are listed too. Off by default:
    /// the list is normally what the sitter still has to act on.
    /// </summary>
    public bool ShowPaidServices { get; set; }

    /// <summary>Gets or sets the month the list is scoped to, or the whole-year entry.</summary>
    public MonthOption? SelectedMonth { get; set; }

    /// <summary>Gets or sets the year the list is scoped to.</summary>
    public int SelectedYear { get; set; }

    public ObservableCollection<MonthOption> MonthOptions { get; } = [];

    /// <summary>Gets the years this dog has services in, plus the current one.</summary>
    public ObservableCollection<int> YearOptions { get; } = [];

    public ObservableCollection<FutureServiceRow> FutureServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded: this screen is shown by assigning
    /// CurrentView.ViewShown rather than by pushing it, so it is never RunAsync'd and
    /// OnRunStarting never fires. OnLoaded also re-runs each time the screen is reopened,
    /// which is what picks up a newly selected dog on the reused presenter instance.
    /// </summary>
    public async Task ReloadAsync()
    {
        // Before the early returns below, so arriving here always lands on the reading state —
        // leaving mid-edit and coming back should not resume a half-finished form. Discarding
        // the photo too stops a picked-then-abandoned file from being orphaned on disk.
        DiscardUnsavedPhoto();
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;

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
        storedPhotoFileName = dog.Image ?? string.Empty;
        PhotoFileName = storedPhotoFileName;

        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        TutorOptions.Clear();
        foreach (var option in tutors)
        {
            TutorOptions.Add(new TutorOption(option.TutorId, option.Name));
        }

        storedTutorId = dog.TutorId;
        var owner = TutorOptions.FirstOrDefault(o => o.Id == dog.TutorId);
        EditTutor = owner;
        OwnerName = owner?.Label ?? string.Empty;

        var services = await repositoryServices.ListForDogAsync(session.CurrentPetSitterId, dogId).WithSync();

        RefreshYearOptions(services);

        // Scoped to the chosen period, and to what is still owed unless the user asks for the
        // settled ones too. Newest first, because browsing a past month is looking at history.
        var future = services
            .Where(s => ServicePeriod.Matches(s, SelectedMonth, SelectedYear))
            .Where(s => ShowPaidServices || !s.ServicePaid)
            .OrderByDescending(s => s.Date)
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

            FutureServices.Add(new FutureServiceRow(
                AppSession.TypeLabel(service.Kind),
                AppSession.DateTimeLabel(service.Date, service.Kind),
                service.ServicePaid,
                service.ServicePaid ? "Pago" : "Pendente",
                service.ServiceDone,
                service.ServiceDone ? "Feito" : "A fazer",
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

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever ShowPaidServices changes.</summary>
    protected void OnShowPaidServicesChanged() => ReloadIfIdle();

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedMonth changes.</summary>
    protected void OnSelectedMonthChanged() => ReloadIfIdle();

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedYear changes.</summary>
    protected void OnSelectedYearChanged() => ReloadIfIdle();

    private void ReloadIfIdle()
    {
        if (!rebuildingOptions)
        {
            AppSession.FireAndForget(ReloadAsync());
        }
    }

    /// <summary>
    /// Keeps the year picker to years this dog actually has services in, plus the current one.
    /// Rebuilt in place under the guard so re-selecting the same value does not reload again.
    /// </summary>
    private void RefreshYearOptions(ServiceItem[] services)
    {
        var years = ServicePeriod.Years(services);
        if (YearOptions.SequenceEqual(years))
        {
            return;
        }

        rebuildingOptions = true;
        try
        {
            var kept = years.Contains(SelectedYear) ? SelectedYear : years[0];

            YearOptions.Clear();
            foreach (var year in years)
            {
                YearOptions.Add(year);
            }

            SelectedYear = kept;

            // The item did not exist when the binding first ran, so the ComboBox has nothing
            // selected; re-announcing the unchanged value is what makes it resolve the entry.
            OnPropertyChanged(nameof(SelectedYear));
        }
        finally
        {
            rebuildingOptions = false;
        }
    }

    /// <summary>
    /// Opens the tapped service. CurrentView keeps a back stack, so the service screen's own Back
    /// returns here rather than to the dogs list.
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
        // Seeded from the loaded record rather than from whatever the inputs held last time, so
        // cancelling and reopening the editor starts from the saved values again.
        EditName = Name;
        EditBreed = Breed;
        EditDescription = Description == "Sem descrição." ? string.Empty : Description;
        EditTutor = TutorOptions.FirstOrDefault(o => o.Id == storedTutorId);
        PhotoFileName = storedPhotoFileName;
        EditError = string.Empty;
        IsEditing = true;
        return Task.CompletedTask;
    }

    private Task CancelEdit()
    {
        DiscardUnsavedPhoto();
        EditError = string.Empty;
        IsEditing = false;
        return Task.CompletedTask;
    }

    private async Task SaveEdit()
    {
        if (session.SelectedDogId is not int dogId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            EditError = "Informe o nome do cachorro.";
            return;
        }

        if (EditTutor == null)
        {
            EditError = "Selecione o tutor.";
            return;
        }

        var result = await repositoryDogs.Update(new Dogs
        {
            DogId = dogId,
            TutorId = EditTutor.Id,
            Name = EditName.Trim(),
            Breed = EditBreed.Trim(),
            Description = EditDescription.Trim(),
            Image = string.IsNullOrEmpty(PhotoFileName) ? null : PhotoFileName,
        }).WithSync();

        if (result != Response.Successful)
        {
            EditError = "Não foi possível salvar as alterações.";
            return;
        }

        // Only now is the replaced photo unreferenced, so this is where it can go.
        if (storedPhotoFileName != PhotoFileName)
        {
            DogImageStore.Delete(storedPhotoFileName);
            storedPhotoFileName = PhotoFileName;
        }

        IsEditing = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
    }

    private async Task ChoosePhoto()
    {
        using var picked = await imagePicker.PickAsync().WithSync();
        if (picked == null)
        {
            return;
        }

        DiscardUnsavedPhoto();
        PhotoFileName = await DogImageStore.SaveAsync(picked.Content, picked.Extension).WithSync();
    }

    private Task RemovePhoto()
    {
        DiscardUnsavedPhoto();
        PhotoFileName = string.Empty;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Drops a photo that was picked but never saved. The stored one is left alone — it is still
    /// what the database points at until Save says otherwise.
    /// </summary>
    private void DiscardUnsavedPhoto()
    {
        if (PhotoFileName != storedPhotoFileName)
        {
            DogImageStore.Delete(PhotoFileName);
        }

        PhotoFileName = storedPhotoFileName;
    }

    private async Task Delete()
    {
        if (session.SelectedDogId is not int dogId)
        {
            return;
        }

        await repositoryDogs.Delete(dogId).WithSync();
        DogImageStore.Delete(storedPhotoFileName);
        session.SelectedDogId = null;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }
}