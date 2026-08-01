using System.Windows.Input;
using PropertyChanged;
using Verion.Framework.Aplicacao.Messaging;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Extensions;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class LoginViewModel : PresentationModelBase<Void, Void>
{
    private readonly Bus bus;
    private readonly NavigationController navigationController;
    private readonly MessageDialog messageDialog;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly SynchronizationContext synchronizationContext;
    private readonly Factory<PresenterBase<SignUpViewModel, Void, Void>> signUpViewFactory;
    private readonly Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory;

    public LoginViewModel(
        Bus bus,
        NavigationController navigationController,
        MessageDialog messageDialog,
        RepositoryPetSitter repositoryPetSitter,
        SynchronizationContext synchronizationContext,
        Factory<PresenterBase<SignUpViewModel, Void, Void>> signUpViewFactory,
        Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory)
    {
        this.bus = bus;
        this.navigationController = navigationController;
        this.messageDialog = messageDialog;
        this.repositoryPetSitter = repositoryPetSitter;
        this.synchronizationContext = synchronizationContext;
        this.signUpViewFactory = signUpViewFactory;
        this.mainViewFactory = mainViewFactory;
        LoginCommand = new SynchronizedCommand(LoginFunction, SynchronizationBehavior.Discard, true);
        SignUpCommand = new SynchronizedCommand(SignUpCommandFunction, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

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
        var response = repositoryPetSitter.VerifyLogin(Email, Password);
        if (response == Response.Successful)
        {
            await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
        }
        else
        {
            await messageDialog.ShowAsync(response.GetDescription(), "Error").WithSync();
        }
    }

    private async Task SignUpCommandFunction() => await navigationController.PushAsync(signUpViewFactory.Create()).WithSync();
}