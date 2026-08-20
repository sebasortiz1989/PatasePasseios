using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class MainViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly AppSession session;
    private readonly CloudBackupService cloudBackup;
    private readonly EventHandler logoutHandler;
    private readonly PresenterBase<DogsViewModel, Unit, Unit> dogsView;
    private readonly PresenterBase<TutorsViewModel, Unit, Unit> tutorsView;
    private readonly PresenterBase<AgendaViewModel, Unit, Unit> homeView;
    private readonly PresenterBase<ServicesViewModel, Unit, Unit> servicesView;
    private readonly PresenterBase<UsersViewModel, Unit, Unit> usersView;

    public MainViewModel(
        NavigationController navigationController,
        AppSession session,
        CloudBackupService cloudBackup,
        CurrentView currentView,
        Factory<PresenterBase<DogsViewModel, Unit, Unit>> dogsViewFactory,
        Factory<PresenterBase<TutorsViewModel, Unit, Unit>> tutorsViewFactory,
        Factory<PresenterBase<AgendaViewModel, Unit, Unit>> homeViewFactory,
        Factory<PresenterBase<ServicesViewModel, Unit, Unit>> servicesViewFactory,
        Factory<PresenterBase<UsersViewModel, Unit, Unit>> usersViewFactory)
    {
        CurrentView = currentView;
        this.cloudBackup = cloudBackup;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);

        // The Perfil tab has no handle on this screen's navigation entry, so it raises a logout
        // request on the session instead. Unsubscribed in OnRunFinishing so repeated
        // login/logout cycles don't leave earlier MainViewModels listening on the singleton.
        this.session = session;
        logoutHandler = (_, _) =>
        {
            session.SignOut();
            BackCommand.Execute(null);
        };
        session.LogoutRequested += logoutHandler;
        dogsView = dogsViewFactory.Create();
        tutorsView = tutorsViewFactory.Create();
        homeView = homeViewFactory.Create();
        servicesView = servicesViewFactory.Create();
        usersView = usersViewFactory.Create();

        // ShowRoot rather than assigning ViewShown: a tab is the bottom of the back stack, so
        // switching tabs must discard any detail screens opened from the previous one.
        DogsViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(dogsView), SynchronizationBehavior.Discard, true);
        TutorsViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(tutorsView), SynchronizationBehavior.Discard, true);
        HomeViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(homeView), SynchronizationBehavior.Discard, true);
        ServicesViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(servicesView), SynchronizationBehavior.Discard, true);
        UsersViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(usersView), SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand DogsViewCommand { get; }

    public ICommand TutorsViewCommand { get; }

    public ICommand HomeViewCommand { get; }

    public ICommand ServicesViewCommand { get; }

    public ICommand UsersViewCommand { get; }

    public CurrentView CurrentView { get; set; }

    /// <summary>
    /// Gets the "shall I back up?" question, put up shortly after login when one is overdue.
    /// </summary>
    public ConfirmRequest BackupRequest { get; } = new();

    protected override Task OnRunStarting(Unit input)
    {
        HomeViewCommand.Execute(null);

        // Not awaited: the archive is the whole database plus every photo, and the first screen
        // must not wait on it. The dialog appears over whichever tab is already showing.
        AppSession.FireAndForget(OfferBackupAsync());
        return Task.CompletedTask;
    }

    protected override Task OnRunFinishing()
    {
        session.LogoutRequested -= logoutHandler;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asks whether to back up when one is overdue, and runs it if the answer is yes.
    /// </summary>
    /// <remarks>
    /// The outcome is deliberately not reported anywhere. This runs unprompted at login, and a
    /// sitter opening the app to check the morning's walks should not have to dismiss a notice
    /// about a background chore that worked. Perfil is where the state of backups is on show, and
    /// where running one by hand reports properly.
    /// </remarks>
    private async Task OfferBackupAsync()
    {
        if (!await cloudBackup.IsDueAsync().NoSync())
        {
            return;
        }

        var confirmed = await BackupRequest
            .AskAsync($"Seu último backup tem mais de uma semana. Enviar uma cópia dos seus dados para a {cloudBackup.DestinationName} agora?")
            .WithSync();

        if (!confirmed)
        {
            await cloudBackup.DeferAsync().NoSync();
            return;
        }

        await cloudBackup.RunAsync().NoSync();
    }
}