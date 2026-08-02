using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Mensagens.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class DogDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryServices repositoryServices;
    private readonly ImagePicker imagePicker;
    private readonly AppSession session;

    /// <summary>
    /// The photo file name currently in the database, as opposed to <see cref="PhotoFileName"/>
    /// which is what the open editor would save. The two differ while a new photo has been picked
    /// but not yet saved, which is what lets Cancel put the old one back.
    /// </summary>
    private string storedPhotoFileName = string.Empty;

    /// <summary>The tutor the record was loaded with, so reopening the editor after a cancelled
    /// edit starts from the saved owner rather than the one that was picked and abandoned.</summary>
    private int storedTutorId;

    public DogDetailViewModel(
        CurrentView currentView,
        NavigationController navigationController,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        RepositoryServices repositoryServices,
        ImagePicker imagePicker,
        AppSession session)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryServices = repositoryServices;
        this.imagePicker = imagePicker;
        this.session = session;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
        ChoosePhotoCommand = new SynchronizedCommand(ChoosePhoto, SynchronizationBehavior.Discard, true);
        RemovePhotoCommand = new SynchronizedCommand(RemovePhoto, SynchronizationBehavior.Discard, true);
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

    public bool NotConfirmingDelete => !ConfirmingDelete;

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

    public ObservableCollection<FutureServiceRow> FutureServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded: this screen is shown by assigning
    /// CurrentView.ViewShown rather than by pushing it, so it is never RunAsync'd and
    /// OnRunStarting never fires. OnLoaded also re-runs each time the screen is reopened,
    /// which is what picks up a newly selected dog on the reused presenter instance.
    /// </summary>
    public async Task ReloadAsync()
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

        // A reload means a different dog or a fresh read, so any half-finished edit is dropped.
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;

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

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

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