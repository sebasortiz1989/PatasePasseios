using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.Mensagens.Dapper;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Dtos;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Session;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class NewTutorViewModel : PresentationModelBase<Void, Void>
{
    private readonly RepositoryTutors repositoryTutors;
    private readonly AppSession session;
    private readonly NavigationController navigationController;

    public NewTutorViewModel(
        NavigationController navigationController,
        RepositoryTutors repositoryTutors,
        AppSession session)
    {
        this.repositoryTutors = repositoryTutors;
        this.session = session;
        this.navigationController = navigationController;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        SaveCommand = new SynchronizedCommand(Save, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand SaveCommand { get; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    protected override Task OnRunStarting(Void input) => Task.CompletedTask;

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "Preencha nome e telefone.";
            return;
        }

        var result = await repositoryTutors.AddForPetSitterAsync(
            new Tutors
            {
                Name = Name.Trim(),
                Telephone = Phone.Trim(),
                Address = Neighborhood.Trim()
            },
            session.CurrentPetSitterId).WithSync();

        if (result != Response.Successful)
        {
            ErrorMessage = "Não foi possível salvar o tutor.";
            return;
        }

        ErrorMessage = string.Empty;
        session.NotifyDataChanged();
        await navigationController.PopAsync(this).WithSync();
    }
}
