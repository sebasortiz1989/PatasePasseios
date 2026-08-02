using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class IncomeRow(string label, string amount)
{
    public string Label { get; } = label;

    public string Amount { get; } = amount;
}

[AddINotifyPropertyChangedInterface]
public class UsersViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;

    public UsersViewModel(
        RepositoryServices repositoryServices,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        AppSession session)
    {
        this.repositoryServices = repositoryServices;
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.session = session;

        // Billing totals depend on services marked paid elsewhere (Agenda, service detail).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        OpenPasswordFormCommand = new SynchronizedCommand(OpenPasswordForm, SynchronizationBehavior.Discard, true);
        CancelPasswordFormCommand = new SynchronizedCommand(() => ShowPasswordForm = false, SynchronizationBehavior.Discard, true);
        SavePasswordCommand = new SynchronizedCommand(SavePassword, SynchronizationBehavior.Discard, true);
        LogoutCommand = new SynchronizedCommand(session.RequestLogout, SynchronizationBehavior.Discard, true);

        CurrentMonthLabel = DateTime.Now.ToString("MMMM 'de' yyyy", new CultureInfo("pt-BR"));
    }

    public ICommand OpenPasswordFormCommand { get; }

    public ICommand CancelPasswordFormCommand { get; }

    public ICommand SavePasswordCommand { get; }

    public ICommand LogoutCommand { get; }

    public string CurrentUserName { get; private set; } = string.Empty;

    public bool ShowPasswordForm { get; set; }

    public bool ShowPasswordSummary => !ShowPasswordForm;

    public string NewPw { get; set; } = string.Empty;

    public string ConfirmPw { get; set; } = string.Empty;

    public string PwMsg { get; set; } = string.Empty;

    public bool HasPwMsg => !string.IsNullOrEmpty(PwMsg);

    public string CurrentMonthLabel { get; }

    public string MonthTotalLabel { get; private set; } = string.Empty;

    public ObservableCollection<IncomeRow> IncomeBreakdown { get; } = [];

    // Portfolio counters, so Perfil summarises the whole account and not just its billing.
    public string DogCountLabel { get; private set; } = string.Empty;

    public string TutorCountLabel { get; private set; } = string.Empty;

    public string ServiceCountLabel { get; private set; } = string.Empty;

    public string PendingCountLabel { get; private set; } = string.Empty;

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        return Task.CompletedTask;
    }

    private void OpenPasswordForm()
    {
        NewPw = string.Empty;
        ConfirmPw = string.Empty;
        PwMsg = string.Empty;
        ShowPasswordForm = true;
    }

    private void SavePassword()
    {
        if (string.IsNullOrEmpty(NewPw) || NewPw.Length < 4)
        {
            PwMsg = "A senha deve ter ao menos 4 caracteres.";
            return;
        }

        if (NewPw != ConfirmPw)
        {
            PwMsg = "As senhas não coincidem.";
            return;
        }

        // Changing the stored password hash isn't wired up yet — the repository has no Update.
        PwMsg = "Alteração de senha ainda não disponível.";
    }

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        CurrentUserName = session.CurrentUserName;

        var now = DateTime.Now;
        var income = await repositoryServices.GetMonthlyIncomeAsync(session.CurrentPetSitterId, now.Year, now.Month).WithSync();
        var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        MonthTotalLabel = AppSession.Money(income.Total);

        IncomeBreakdown.Clear();
        IncomeBreakdown.Add(new IncomeRow("Passeio", AppSession.Money(income.Walk)));
        IncomeBreakdown.Add(new IncomeRow("Pet sitting", AppSession.Money(income.Sitting)));
        IncomeBreakdown.Add(new IncomeRow("Hotel", AppSession.Money(income.Hotel)));

        DogCountLabel = dogs.Length.ToString(CultureInfo.InvariantCulture);
        TutorCountLabel = tutors.Length.ToString(CultureInfo.InvariantCulture);
        ServiceCountLabel = services.Length.ToString(CultureInfo.InvariantCulture);
        PendingCountLabel = services.Count(s => !s.ServicePaid).ToString(CultureInfo.InvariantCulture);
    }
}
