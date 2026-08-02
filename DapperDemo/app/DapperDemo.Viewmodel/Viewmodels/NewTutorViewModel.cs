using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class NewTutorViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryTutors repositoryTutors;
    private readonly AppSession session;
    private readonly CurrentView currentView;

    public NewTutorViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        AppSession session)
    {
        this.repositoryTutors = repositoryTutors;
        this.session = session;
        this.currentView = currentView;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        SaveCommand = new SynchronizedCommand(Save, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand SaveCommand { get; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    protected override Task OnRunStarting(Unit input) => Task.CompletedTask;

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
        currentView.GoBack();
    }
}
