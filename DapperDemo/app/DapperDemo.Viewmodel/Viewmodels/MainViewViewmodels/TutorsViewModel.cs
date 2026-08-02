using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Session;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public sealed class TutorRow(string initials, string name, string subtitle, ICommand openCommand) : IDisposable
{
    public string Initials { get; } = initials;

    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}

[AddINotifyPropertyChangedInterface]
public class TutorsViewModel : PresentationModelBase<Void, Void>
{
    private readonly NavigationController navigationController;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;
    private readonly Factory<PresenterBase<TutorDetailViewModel, Void, Void>> tutorDetailFactory;
    private readonly Factory<PresenterBase<NewTutorViewModel, Void, Void>> newTutorFactory;

    public TutorsViewModel(
        NavigationController navigationController,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        AppSession session,
        Factory<PresenterBase<TutorDetailViewModel, Void, Void>> tutorDetailFactory,
        Factory<PresenterBase<NewTutorViewModel, Void, Void>> newTutorFactory)
    {
        this.navigationController = navigationController;
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.session = session;
        this.tutorDetailFactory = tutorDetailFactory;
        this.newTutorFactory = newTutorFactory;

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

    protected override async Task OnRunStarting(Void input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        ClearRows();
        return Task.CompletedTask;
    }

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
            var openCommand = new SynchronizedCommand(() => Open(tutor.TutorId), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
            TutorsCollection.Add(new TutorRow(AppSession.Initials(tutor.Name), tutor.Name, $"{neighbourhood}{dogCountLabel}", openCommand));
        }

        TutorCountLabel = tutors.Length == 1 ? "1 cadastrado" : $"{tutors.Length} cadastrados";
        IsEmpty = tutors.Length == 0;
    }

    private void ClearRows()
    {
        foreach (var row in TutorsCollection)
        {
            row.Dispose();
        }

        TutorsCollection.Clear();
    }

    private async Task Open(int tutorId)
    {
        session.SelectedTutorId = tutorId;
        await navigationController.PushAsync(tutorDetailFactory.Create()).WithSync();
    }

    private async Task OpenNewTutor() => await navigationController.PushAsync(newTutorFactory.Create()).WithSync();
}
