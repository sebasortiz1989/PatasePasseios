using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorsViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly CurrentView currentView;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;
    private readonly PresenterBase<TutorDetailViewModel, Unit, Unit> tutorDetailView;
    private readonly PresenterBase<NewTutorViewModel, Unit, Unit> newTutorView;

    public TutorsViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        AppSession session,
        Factory<PresenterBase<TutorDetailViewModel, Unit, Unit>> tutorDetailFactory,
        Factory<PresenterBase<NewTutorViewModel, Unit, Unit>> newTutorFactory)
    {
        this.currentView = currentView;
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.session = session;

        // CurrentView.ViewShown is typed `object` and is bound straight to a ContentControl, so it
        // needs the created presenter — assigning the factory itself compiles but renders blank.
        tutorDetailView = tutorDetailFactory.Create();
        newTutorView = newTutorFactory.Create();

        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        AddTutorCommand = new SynchronizedCommand(OpenNewTutor, SynchronizationBehavior.Discard, true);
    }

    public ICommand AddTutorCommand { get; }

    public ObservableCollection<TutorRow> TutorsCollection { get; } = [];

    public string TutorCountLabel { get; private set; } = string.Empty;

    // Defaults to true so the screen shows its empty-state message rather than nothing at all
    // in the moment before the first load completes.
    public bool IsEmpty { get; private set; } = true;

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        ClearRows();
        foreach (var tutor in tutors)
        {
            var dogCount = dogs.Count(d => d.TutorId == tutor.TutorId);
            var dogCountLabel = dogCount == 1 ? "1 cachorro" : $"{dogCount} cachorros";
            var neighbourhood = string.IsNullOrWhiteSpace(tutor.Address) ? string.Empty : $"{tutor.Address} · ";

            // CA2000: ownership passes to the TutorRow, which disposes the command when the list rebuilds.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(() => Open(tutor.TutorId, tutor.Name), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
            TutorsCollection.Add(new TutorRow(AppSession.Initials(tutor.Name), tutor.Name, $"{neighbourhood}{dogCountLabel}", openCommand));
        }

        TutorCountLabel = tutors.Length == 1 ? "1 cadastrado" : $"{tutors.Length} cadastrados";
        IsEmpty = tutors.Length == 0;
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
        foreach (var row in TutorsCollection)
        {
            row.Dispose();
        }

        TutorsCollection.Clear();
    }

    /// <summary>
    /// Opens one tutor's screen, labelled with their name so anything opened from there goes back
    /// to that person rather than to a generic word.
    /// </summary>
    private Task Open(int tutorId, string name)
    {
        session.SelectedTutorId = tutorId;
        currentView.Show(tutorDetailView, name);
        return Task.CompletedTask;
    }

    private Task OpenNewTutor()
    {
        currentView.Show(newTutorView, "Novo tutor");
        return Task.CompletedTask;
    }
}