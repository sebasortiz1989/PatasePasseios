using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class MainViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly AppSession session;
    private readonly EventHandler logoutHandler;
    private readonly PresenterBase<DogsViewModel, Unit, Unit> dogsView;
    private readonly PresenterBase<TutorsViewModel, Unit, Unit> tutorsView;
    private readonly PresenterBase<AgendaViewModel, Unit, Unit> homeView;
    private readonly PresenterBase<ServicesViewModel, Unit, Unit> servicesView;
    private readonly PresenterBase<UsersViewModel, Unit, Unit> usersView;

    public MainViewModel(
        NavigationController navigationController,
        AppSession session,
        CurrentView currentView,
        Factory<PresenterBase<DogsViewModel, Unit, Unit>> dogsViewFactory,
        Factory<PresenterBase<TutorsViewModel, Unit, Unit>> tutorsViewFactory,
        Factory<PresenterBase<AgendaViewModel, Unit, Unit>> homeViewFactory,
        Factory<PresenterBase<ServicesViewModel, Unit, Unit>> servicesViewFactory,
        Factory<PresenterBase<UsersViewModel, Unit, Unit>> usersViewFactory)
    {
        CurrentView = currentView;
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
        DogsViewCommand = new SynchronizedCommand(() => CurrentView.ViewShown = dogsView, SynchronizationBehavior.Discard, true);
        TutorsViewCommand = new SynchronizedCommand(() => CurrentView.ViewShown = tutorsView, SynchronizationBehavior.Discard, true);
        HomeViewCommand = new SynchronizedCommand(() => CurrentView.ViewShown = homeView, SynchronizationBehavior.Discard, true);
        ServicesViewCommand = new SynchronizedCommand(() => CurrentView.ViewShown = servicesView, SynchronizationBehavior.Discard, true);
        UsersViewCommand = new SynchronizedCommand(() => CurrentView.ViewShown = usersView, SynchronizationBehavior.Discard, true);
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
        return Task.CompletedTask;
    }

    protected override Task OnRunFinishing()
    {
        session.LogoutRequested -= logoutHandler;
        return Task.CompletedTask;
    }
}