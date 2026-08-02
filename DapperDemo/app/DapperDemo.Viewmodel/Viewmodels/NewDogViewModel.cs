using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class NewDogViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly AppSession session;

    public NewDogViewModel(
        CurrentView currentView,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        AppSession session)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.session = session;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        SaveCommand = new SynchronizedCommand(Save, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand SaveCommand { get; }

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
        // the fields would still hold the dog added last time.
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
        }).WithSync();

        if (result != Response.Successful)
        {
            ErrorMessage = "Não foi possível salvar o cachorro.";
            return;
        }

        ErrorMessage = string.Empty;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }
}