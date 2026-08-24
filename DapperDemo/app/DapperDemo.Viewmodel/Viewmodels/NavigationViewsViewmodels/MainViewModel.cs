using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;
using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class MainViewModel : PresentationModelBase<Unit, Unit>
{
    /// <summary>
    /// How often the daily copy is looked for while the app stays open.
    /// </summary>
    /// <remarks>
    /// A check is a small JSON read, so this can be frequent; the archive itself is built at most
    /// once a day. Without the loop the schedule would only ever be honoured by whoever happened
    /// to sign in after eight — a sitter who leaves the app open all day would never be backed up.
    /// </remarks>
    private static readonly TimeSpan BackupCheckInterval = TimeSpan.FromMinutes(15);

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
        // The label each tab is known by, which is what the first detail screen opened from it
        // shows on its back control.
        DogsViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(dogsView, "Cachorros"), SynchronizationBehavior.Discard, true);
        TutorsViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(tutorsView, "Tutores"), SynchronizationBehavior.Discard, true);
        HomeViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(homeView, "Agenda"), SynchronizationBehavior.Discard, true);
        ServicesViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(servicesView, "Serviços"), SynchronizationBehavior.Discard, true);
        UsersViewCommand = new SynchronizedCommand(() => CurrentView.ShowRoot(usersView, "Perfil"), SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand DogsViewCommand { get; }

    public ICommand TutorsViewCommand { get; }

    public ICommand HomeViewCommand { get; }

    public ICommand ServicesViewCommand { get; }

    public ICommand UsersViewCommand { get; }

    public CurrentView CurrentView { get; set; }

    protected override Task OnRunStarting(Unit input)
    {
        HomeViewCommand.Execute(null);

        // Not awaited: the archive is the whole database plus every photo, and the first screen
        // must not wait on it.
        AppSession.FireAndForget(WatchBackupScheduleAsync());
        return Task.CompletedTask;
    }

    protected override Task OnRunFinishing()
    {
        session.LogoutRequested -= logoutHandler;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Takes the daily copy whenever it comes due, for as long as this session lasts.
    /// </summary>
    /// <remarks>
    /// Once at sign-in — the ordinary case, where the app is opened in the morning and the copy is
    /// taken before the day's records start changing — and then on a timer, so an app left open
    /// across eight o'clock is backed up too. The loop ends with the screen: PresentationModelFinished
    /// is the framework's token for exactly this — work that must not outlive the run.
    /// </remarks>
    private async Task WatchBackupScheduleAsync()
    {
        var finished = PresentationModelFinished;

        await RunDailyBackupAsync().NoSync();

        try
        {
            using var timer = new PeriodicTimer(BackupCheckInterval);
            while (await timer.WaitForNextTickAsync(finished).NoSync())
            {
                await RunDailyBackupAsync().NoSync();
            }
        }
        catch (OperationCanceledException)
        {
            // Signing out. The loop only exists while someone is signed in.
        }
    }

    /// <summary>
    /// Sends a copy if today's is still owed, and says nothing either way.
    /// </summary>
    /// <remarks>
    /// Silent on purpose. This runs unprompted, and a sitter opening the app to check the
    /// morning's walks should not have to dismiss a notice about a background chore. Perfil is
    /// where the state of backups is on show, and where a backup the sitter <em>asked</em> for
    /// reports what happened.
    /// </remarks>
    private async Task RunDailyBackupAsync()
    {
        if (!await cloudBackup.IsDueAsync().NoSync())
        {
            return;
        }

        // Nothing to send until a folder has been chosen in Perfil. Checked rather than assumed:
        // a folder can be deleted or its permission revoked between one launch and the next.
        if (!await cloudBackup.IsLinkedAsync().NoSync())
        {
            return;
        }

        await cloudBackup.RunAsync().NoSync();
    }
}