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
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class NewDogViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly ImagePicker imagePicker;
    private readonly AppSession session;

    public NewDogViewModel(
        CurrentView currentView,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        ImagePicker imagePicker,
        AppSession session)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.imagePicker = imagePicker;
        this.session = session;
        Navigation = currentView;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        SaveCommand = new SynchronizedCommand(Save, SynchronizationBehavior.Discard, true);
        ChoosePhotoCommand = new SynchronizedCommand(ChoosePhoto, SynchronizationBehavior.Discard, true);
        RemovePhotoCommand = new SynchronizedCommand(RemovePhoto, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    /// <summary>Gets the navigator, so the back control can name the screen it returns to.</summary>
    public CurrentView Navigation { get; }

    public ICommand SaveCommand { get; }

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    public ObservableCollection<TutorOption> TutorOptions { get; } = [];

    /// <summary>
    /// Gets a value indicating whether a dog needs an owner, so with no tutors yet the form points
    /// the user there first. Defaults to true so the empty state shows while the list loads,
    /// rather than a picker with nothing in it.
    /// </summary>
    public bool HasNoTutors { get; private set; } = true;

    public bool HasTutors => !HasNoTutors;

    public TutorOption? SelectedTutor { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the photo's file name in the store, or empty for none.
    /// </summary>
    /// <remarks>
    /// The file is copied in as soon as it is picked rather than on save, so the form can show a
    /// preview. Abandoning the form deletes it again — see <see cref="ReloadAsync"/>.
    /// </remarks>
    public string PhotoFileName { get; set; } = string.Empty;

    /// <summary>Gets the photo's full path for the preview, or null when there is none.</summary>
    public string? PhotoPath => DogImageStore.ResolvePath(PhotoFileName);

    public bool HasPhoto => PhotoPath != null;

    public bool NoPhoto => !HasPhoto;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Public because the View calls it from OnLoaded: this screen is shown by assigning
    /// CurrentView.ViewShown rather than by pushing it, so it is never RunAsync'd and
    /// OnRunStarting never fires. Without this the tutor picker stays empty. OnLoaded also
    /// re-runs on every reopen, so a tutor added since last time shows up.
    /// </summary>
    public async Task ReloadAsync()
    {
        // The form starts blank on every open: the presenter instance is reused, so without this
        // the fields would still hold the dog added last time. A photo picked and then abandoned
        // is already sitting in the store, so it goes too rather than accumulating on disk.
        DogImageStore.Delete(PhotoFileName);
        PhotoFileName = string.Empty;
        Name = string.Empty;
        Breed = string.Empty;
        Description = string.Empty;
        ErrorMessage = string.Empty;
        SelectedTutor = null;

        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        TutorOptions.Clear();
        foreach (var tutor in tutors)
        {
            TutorOptions.Add(new TutorOption(tutor.TutorId, tutor.Name));
        }

        HasNoTutors = TutorOptions.Count == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    private async Task ChoosePhoto()
    {
        using var picked = await imagePicker.PickAsync().WithSync();
        if (picked == null)
        {
            return;
        }

        // Replacing a preview leaves the previous copy orphaned, so it goes first.
        DogImageStore.Delete(PhotoFileName);
        PhotoFileName = await DogImageStore.SaveAsync(picked.Content, picked.Extension).WithSync();
    }

    private Task RemovePhoto()
    {
        DogImageStore.Delete(PhotoFileName);
        PhotoFileName = string.Empty;
        return Task.CompletedTask;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Informe o nome do cachorro.";
            return;
        }

        if (SelectedTutor == null)
        {
            ErrorMessage = "Selecione o tutor.";
            return;
        }

        var result = await repositoryDogs.Add(new Dogs
        {
            TutorId = SelectedTutor.Id,
            Name = Name.Trim(),
            Breed = Breed.Trim(),
            Description = Description.Trim(),
            Image = string.IsNullOrEmpty(PhotoFileName) ? null : PhotoFileName,
        }).WithSync();

        if (result != Response.Successful)
        {
            ErrorMessage = "Não foi possível salvar o cachorro.";
            return;
        }

        // The row now owns the file, so clearing this stops the form reset from deleting it.
        PhotoFileName = string.Empty;
        ErrorMessage = string.Empty;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }
}