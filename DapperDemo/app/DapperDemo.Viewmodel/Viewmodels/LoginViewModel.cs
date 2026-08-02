using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Extensions;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class LoginViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly NavigationController navigationController;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly SynchronizationContext synchronizationContext;
    private readonly AppSession session;
    private readonly Factory<PresenterBase<SignUpViewModel, Unit, Unit>> signUpViewFactory;
    private readonly Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory;

    public LoginViewModel(
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter,
        SynchronizationContext synchronizationContext,
        AppSession session,
        Factory<PresenterBase<SignUpViewModel, Unit, Unit>> signUpViewFactory,
        Factory<PresenterBase<MainViewModel, Unit, Unit>> mainViewFactory)
    {
        this.navigationController = navigationController;
        this.repositoryPetSitter = repositoryPetSitter;
        this.synchronizationContext = synchronizationContext;
        this.session = session;
        this.signUpViewFactory = signUpViewFactory;
        this.mainViewFactory = mainViewFactory;
        LoginCommand = new SynchronizedCommand(LoginFunction, SynchronizationBehavior.Discard, true);
        SignUpCommand = new SynchronizedCommand(SignUpCommandFunction, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string LoginError { get; set; } = string.Empty;

    public bool HasLoginError => !string.IsNullOrEmpty(LoginError);

    public ICommand LoginCommand { get; }

    public ICommand SignUpCommand { get; }

    protected override Task OnRunStarting(Unit input)
    {
        // GetAll fires its callback from a Task.Run, so the assignment to Email — which raises
        // PropertyChanged into a binding — has to be marshalled back onto the UI thread.
        repositoryPetSitter.GetAll(perSitters => synchronizationContext.Run(() =>
        {
            if (perSitters.Length != 0)
            {
                Email = perSitters[^1].Email;
            }
        }));

        return Task.CompletedTask;
    }

    private async Task LoginFunction()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            LoginError = "Preencha email e senha.";
            return;
        }

        var response = repositoryPetSitter.VerifyLogin(Email, Password);
        if (response != Response.Successful)
        {
            LoginError = response.GetDescription();
            return;
        }

        // Everything after login is scoped to this account, so the row has to be loaded before
        // navigating — VerifyLogin only reports success, it doesn't say who signed in.
        var petSitter = await repositoryPetSitter.GetByEmailAsync(Email).WithSync();
        if (petSitter == null)
        {
            LoginError = Response.EmailDoesNotExists.GetDescription();
            return;
        }

        LoginError = string.Empty;
        Password = string.Empty;
        session.SignIn(petSitter);
        await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
    }

    private async Task SignUpCommandFunction() => await navigationController.PushAsync(signUpViewFactory.Create()).WithSync();
}
