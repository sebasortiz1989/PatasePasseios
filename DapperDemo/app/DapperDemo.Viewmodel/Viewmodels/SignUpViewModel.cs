using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Dtos;
using Verion.Treinamento.Mensagens.Dapper.Extensions;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Session;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class SignUpViewModel : PresentationModelBase<Void, Void>
{
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly AppSession session;
    private readonly NavigationController navigationController;
    private readonly Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory;

    public SignUpViewModel(
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter,
        AppSession session,
        Factory<PresenterBase<MainViewModel, Void, Void>> mainViewFactory)
    {
        this.repositoryPetSitter = repositoryPetSitter;
        this.session = session;
        this.navigationController = navigationController;
        this.mainViewFactory = mainViewFactory;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        BirthDate = DateTime.UtcNow - TimeSpan.FromDays(7000);
        RegisterCommand = new SynchronizedCommand(RegisterFunction, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public DateTime MinimumDate { get; } = new(1950, 1, 1);

    public string SignupError { get; set; } = string.Empty;

    public bool HasSignupError => !string.IsNullOrEmpty(SignupError);

    public ICommand BackCommand { get; }

    public ICommand RegisterCommand { get; }

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private async Task RegisterFunction()
    {
        if (Email.IsNullOrEmpty() || Password.IsNullOrEmpty() || Name.IsNullOrEmpty())
        {
            SignupError = "Preencha todos os campos.";
            return;
        }

        var result = await repositoryPetSitter.Add(new PetSitter
        {
            Name = Name,
            Email = Email,
            Password = Password,
            BirthDate = BirthDate,
            PasswordHash = string.Empty
        }).WithSync();

        if (result != Response.Successful)
        {
            SignupError = result.GetDescription();
            return;
        }

        // Read the row back for its generated id: everything the new account creates is scoped to it.
        var petSitter = await repositoryPetSitter.GetByEmailAsync(Email).WithSync();
        if (petSitter == null)
        {
            SignupError = Response.Failed.GetDescription();
            return;
        }

        SignupError = string.Empty;
        Password = string.Empty;
        session.SignIn(petSitter);
        await navigationController.PushAsync(mainViewFactory.Create()).WithSync();
    }
}
