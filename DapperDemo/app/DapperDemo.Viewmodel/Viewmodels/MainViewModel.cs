using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class MainViewModel : PresentationModelBase<Void, Void>
{
    private readonly NavigationController navigationController;
    private readonly PresenterBase<DogsViewModel, Void, Void> dogsView;
    private readonly PresenterBase<TutorsViewModel, Void, Void> tutorsView;
    private readonly PresenterBase<HomeViewModel, Void, Void> homeView;
    private readonly PresenterBase<ServicesViewModel, Void, Void> servicesView;
    private readonly PresenterBase<UsersViewModel, Void, Void> usersView;

    public MainViewModel(
        NavigationController navigationController,
        Factory<PresenterBase<DogsViewModel, Void, Void>> dogsViewFactory,
        Factory<PresenterBase<TutorsViewModel, Void, Void>> tutorsViewFactory,
        Factory<PresenterBase<HomeViewModel, Void, Void>> homeViewFactory,
        Factory<PresenterBase<ServicesViewModel, Void, Void>> servicesViewFactory,
        Factory<PresenterBase<UsersViewModel, Void, Void>> usersViewFactory)
    {
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        dogsView = dogsViewFactory.Create();
        tutorsView = tutorsViewFactory.Create();
        homeView = homeViewFactory.Create();
        servicesView = servicesViewFactory.Create();
        usersView = usersViewFactory.Create();
        DogsViewCommand = new SynchronizedCommand(() => CurrentView = dogsView, SynchronizationBehavior.Discard, true);
        TutorsViewCommand = new SynchronizedCommand(() => CurrentView = tutorsView, SynchronizationBehavior.Discard, true);
        HomeViewCommand = new SynchronizedCommand(() => CurrentView = homeView, SynchronizationBehavior.Discard, true);
        ServicesViewCommand = new SynchronizedCommand(() => CurrentView = servicesView, SynchronizationBehavior.Discard, true);
        UsersViewCommand = new SynchronizedCommand(() => CurrentView = usersView, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand DogsViewCommand { get; }

    public ICommand TutorsViewCommand { get; }

    public ICommand HomeViewCommand { get; }

    public ICommand ServicesViewCommand { get; }

    public ICommand UsersViewCommand { get; }

    public object CurrentView { get; set; }

    protected override Task OnRunStarting(Void input)
    {
        HomeViewCommand.Execute(null);
        return Task.CompletedTask;
    }
}