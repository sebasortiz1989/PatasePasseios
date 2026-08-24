using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using PatasePasseios.Viewmodel.Services;
using PatasePasseios.Viewmodel.Viewmodels.Session;
using PatasePasseios.Viewmodel.Viewmodels.TabViewsViewmodels;
using PatasePasseios.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.NavigationViewsViewmodels;

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

    /// <summary>
    /// Gets the request behind the "choose a backup folder" dialog.
    /// </summary>
    /// <remarks>
    /// Asked here rather than from Perfil because it must reach a sitter who never opens Perfil —
    /// which is exactly the sitter who has no folder set. Bound to a ConfirmDialog in MainView.
    /// </remarks>
    public ConfirmRequest BackupRequest { get; } = new();

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

        // Before the run, not after: a sitter who chooses a folder here has the first copy taken
        // by the very next line rather than tomorrow morning.
        await AskForDestinationAsync().NoSync();
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
    /// Asks for a backup folder once a day, until there is one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one thing about backups that interrupts. Everything else here is silent, which is right
    /// for a chore that is working — but a device with no destination is not backing anything up,
    /// and saying nothing about that is how it went unnoticed until now.
    /// </para>
    /// <para>
    /// After sign-in rather than at startup: there is no one to ask on the login screen, and the
    /// dialog would be answered by whoever got there first rather than by the sitter.
    /// </para>
    /// <para>
    /// <c>WithSync()</c> throughout, unlike the run below it — this one puts a dialog on screen and
    /// opens a folder picker, so every continuation has to land back on the UI thread.
    /// </para>
    /// </remarks>
    private async Task AskForDestinationAsync()
    {
        if (!await cloudBackup.ShouldPromptForDestinationAsync().WithSync())
        {
            return;
        }

        // Stamped before the question, not after the answer. A sitter who closes the app instead of
        // answering has still been asked, and stamping afterwards would put the same dialog back up
        // on the next sign-in — which is the behaviour that makes someone stop reading dialogs.
        await cloudBackup.MarkPromptedAsync().WithSync();

        var accepted = await BackupRequest.AskAsync(
            "O backup automático ainda não está ativado. Escolha uma pasta — no Drive ou no " +
            "aparelho — e o app envia uma cópia de tudo todo dia de manhã, sozinho. " +
            "Quer escolher a pasta agora?").WithSync();

        if (!accepted)
        {
            return;
        }

        // Cancelling the picker needs no message: they are back where they started, and Perfil
        // carries the standing reminder for as long as there is no folder.
        await cloudBackup.LinkAsync().WithSync();
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