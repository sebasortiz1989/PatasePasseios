using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using PatasePasseios.Repository.Dapper;
using PatasePasseios.Repository.Dapper.Aggregates;
using PatasePasseios.Repository.Dapper.Dtos;
using PatasePasseios.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

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

    /// <summary>Gets the navigator, so the back control can name the screen it returns to.</summary>
    public CurrentView Navigation => currentView;

    public ICommand SaveCommand { get; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Clears the form. Called from the View's OnLoaded, so reopening the screen never shows the
    /// values from the tutor added last time — the presenter instance is reused.
    /// </summary>
    public Task ReloadAsync()
    {
        Name = string.Empty;
        Phone = string.Empty;
        Neighborhood = string.Empty;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    protected override Task OnRunStarting(Unit input) => ReloadAsync();

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
                Address = Neighborhood.Trim(),
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