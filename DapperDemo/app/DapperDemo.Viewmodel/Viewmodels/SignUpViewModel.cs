using System.Windows.Input;
using PropertyChanged;
using Verion.Framework.Aplicacao.Messaging;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Dtos;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class SignUpViewModel : PresentationModelBase<Void, Void>
{
    private readonly MessageDialog messageDialog;
    private readonly RepositoryPetSitter _repositoryPetSitter;

    public SignUpViewModel(
        MessageDialog messageDialog,
        NavigationController navigationController,
        RepositoryPetSitter repositoryPetSitter)
    {
        this.messageDialog = messageDialog;
        this._repositoryPetSitter = repositoryPetSitter;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        BirthDate = DateTime.UtcNow - TimeSpan.FromDays(7000);
        RegisterCommand = new SynchronizedCommand(RegisterFunction, SynchronizationBehavior.Discard, true);
    }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public DateTime MinimumDate { get; } = new(1950, 1, 1);

    public ICommand BackCommand { get; }

    public ICommand RegisterCommand { get; }

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private async Task RegisterFunction()
    {
        if (Email.IsNullOrEmpty() || Password.IsNullOrEmpty() || Name.IsNullOrEmpty())
            return;

        var result = await _repositoryPetSitter.Add(new PetSitter
        {
            Name = Name,
            Email = Email,
            Password = Password,
            BirthDate = BirthDate,
            PasswordHash = string.Empty
        }).WithSync();
        await messageDialog.ShowAsync(result.ToString()).WithSync();
    }
}