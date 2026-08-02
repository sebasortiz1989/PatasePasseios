using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

public class TutorOption(int id, string label)
{
    public int Id { get; } = id;

    public string Label { get; } = label;
}

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

    /// <summary>A dog needs an owner, so with no tutors yet the form points the user there first.</summary>
    public bool HasNoTutors { get; private set; }

    public bool HasTutors => !HasNoTutors;

    public TutorOption? SelectedTutor { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    protected override async Task OnRunStarting(Unit input)
    {
        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        TutorOptions.Clear();
        foreach (var tutor in tutors)
        {
            TutorOptions.Add(new TutorOption(tutor.TutorId, tutor.Name));
        }

        HasNoTutors = TutorOptions.Count == 0;
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
            Description = Description.Trim()
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