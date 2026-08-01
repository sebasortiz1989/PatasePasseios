using System.Windows.Input;
using PropertyChanged;
using Verion.Framework.Aplicacao.Messaging;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Extensions;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class LoginViewModel : PresentationModelBase<Void, Void>
{
    private readonly Bus bus;
    private readonly NavigationController navigationController;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly SynchronizationContext synchronizationContext;
    private readonly MockAppData mockAppData;
    private readonly Factory<PresenterBase<SignUpViewModel, Void, Void>> signUpViewFactory;
    private readonly Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory;

    public LoginViewModel(
        Bus bus,
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter,
        SynchronizationContext synchronizationContext,
        MockAppData mockAppData,
        Factory<PresenterBase<SignUpViewModel, Void, Void>> signUpViewFactory,
        Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory)
    {
        this.bus = bus;
        this.navigationController = navigationController;
        this.repositoryPetSitter = repositoryPetSitter;
        this.synchronizationContext = synchronizationContext;
        this.mockAppData = mockAppData;
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

    protected override Task OnRunStarting(Void input)
    {
        repositoryPetSitter.GetAll(perSitters =>
        {
            synchronizationContext.SwitchTo();
            if (perSitters.Length != 0)
            {
                Email = perSitters[^1].Email;
            }
        });

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
        if (response == Response.Successful)
        {
            LoginError = string.Empty;
            repositoryPetSitter.GetAll(petSitters =>
            {
                synchronizationContext.SwitchTo();
                var match = petSitters.FirstOrDefault(p => p.Email == Email);
                if (match != null)
                {
                    mockAppData.SetCurrentUserName(match.Name);
                }
            });
            await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
        }
        else
        {
            LoginError = response.GetDescription();
        }
    }

    private async Task SignUpCommandFunction() => await navigationController.PushAsync(signUpViewFactory.Create()).WithSync();
}