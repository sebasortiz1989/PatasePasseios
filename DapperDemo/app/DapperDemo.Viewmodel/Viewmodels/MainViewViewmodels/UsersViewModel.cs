using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

[AddINotifyPropertyChangedInterface]
public class UsersViewModel : PresentationModelBase<Unit, Unit>
{
    /// <summary>What a hidden amount reads as. Wide enough not to hint at the figure's length.</summary>
    private const string HiddenMoney = "••••••";

    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;

    /// <summary>
    /// The figures behind the labels, kept so toggling the eye can re-render them without
    /// going back to the database.
    /// </summary>
    private MonthlyIncome income = new();

    /// <summary>
    /// The selected month's services, kept so toggling the eye can re-render the per-dog breakdown
    /// without another round trip — the same reason <see cref="income"/> is held.
    /// </summary>
    private ServiceItem[] monthServices = [];

    /// <summary>Guards against the filter reload retriggering itself while it assigns properties.</summary>
    private bool reloading;

    public UsersViewModel(
        RepositoryServices repositoryServices,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        RepositoryPetSitter repositoryPetSitter,
        AppSession session)
    {
        this.repositoryServices = repositoryServices;
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryPetSitter = repositoryPetSitter;
        this.session = session;

        // Billing totals depend on services marked paid elsewhere (Agenda, service detail).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        OpenPasswordFormCommand = new SynchronizedCommand(OpenPasswordForm, SynchronizationBehavior.Discard, true);
        CancelPasswordFormCommand = new SynchronizedCommand(() => ShowPasswordForm = false, SynchronizationBehavior.Discard, true);
        SavePasswordCommand = new SynchronizedCommand(SavePassword, SynchronizationBehavior.Discard, true);
        LogoutCommand = new SynchronizedCommand(session.RequestLogout, SynchronizationBehavior.Discard, true);
        EditProfileCommand = new SynchronizedCommand(StartEditProfile, SynchronizationBehavior.Discard, true);
        CancelProfileCommand = new SynchronizedCommand(CancelEditProfile, SynchronizationBehavior.Discard, true);
        SaveProfileCommand = new SynchronizedCommand(SaveProfile, SynchronizationBehavior.Discard, true);
        ToggleMoneyVisibleCommand = new SynchronizedCommand(ToggleMoneyVisible, SynchronizationBehavior.Discard, true);

        for (var month = 1; month <= 12; month++)
        {
            var name = Brazil.DateTimeFormat.GetMonthName(month);
            MonthOptions.Add(new MonthOption(month, char.ToUpper(name[0], Brazil) + name[1..]));
        }

        var now = DateTime.Now;
        SelectedMonth = MonthOptions[now.Month - 1];
        SelectedYear = now.Year;

        // Reloading on a filter change rather than behind a button: two pickers and an Apply
        // would be three taps to see one month.
        PropertyChanged += ReloadWhenPeriodChanges;
    }

    public ICommand OpenPasswordFormCommand { get; }

    public ICommand CancelPasswordFormCommand { get; }

    public ICommand SavePasswordCommand { get; }

    public ICommand LogoutCommand { get; }

    public ICommand EditProfileCommand { get; }

    public ICommand CancelProfileCommand { get; }

    public ICommand SaveProfileCommand { get; }

    public ICommand ToggleMoneyVisibleCommand { get; }

    public string CurrentUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the Pix key, or a prompt when none has been set yet.</summary>
    public string PixLabel { get; private set; } = string.Empty;

    public string BirthDateLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the profile card shows inputs rather than text.</summary>
    public bool IsEditingProfile { get; private set; }

    public bool IsViewingProfile => !IsEditingProfile;

    public string EditName { get; set; } = string.Empty;

    public string EditPix { get; set; } = string.Empty;

    public DateTime EditBirthDate { get; set; } = DateTime.Now.Date;

    public string ProfileMsg { get; private set; } = string.Empty;

    public bool HasProfileMsg => !string.IsNullOrEmpty(ProfileMsg);

    public bool ShowPasswordForm { get; set; }

    public bool ShowPasswordSummary => !ShowPasswordForm;

    /// <summary>Gets or sets the password being replaced. Required — see RepositoryPetSitter.ChangePasswordAsync.</summary>
    public string CurrentPw { get; set; } = string.Empty;

    public string NewPw { get; set; } = string.Empty;

    public string ConfirmPw { get; set; } = string.Empty;

    public string PwMsg { get; set; } = string.Empty;

    public bool HasPwMsg => !string.IsNullOrEmpty(PwMsg);

    /// <summary>
    /// Gets a value indicating whether the last password message was a success rather than a
    /// complaint, so the view can colour it accordingly.
    /// </summary>
    public bool PwMsgIsError { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether amounts are readable. Off hides them behind bullets, for
    /// looking at the app with someone beside you. Persisted on the account, so closing the app
    /// does not put the figures back on screen.
    /// </summary>
    public bool MoneyVisible { get; private set; } = true;

    public bool MoneyHidden => !MoneyVisible;

    public ObservableCollection<MonthOption> MonthOptions { get; } = [];

    /// <summary>Gets the years worth offering: every year with a service, plus this one.</summary>
    public ObservableCollection<int> YearOptions { get; } = [];

    public MonthOption? SelectedMonth { get; set; }

    public int SelectedYear { get; set; }

    public string MonthTotalLabel { get; private set; } = string.Empty;

    public ObservableCollection<IncomeRow> IncomeBreakdown { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the per-dog breakdown of the month is shown. Off by
    /// default: the headline figures answer "how did the month go", and the detail is for chasing
    /// a specific debt.
    /// </summary>
    public bool ShowMonthDetail { get; set; }

    /// <summary>Gets the money already received in the month, per dog.</summary>
    public ObservableCollection<DogSummaryRow> PaidSummaries { get; } = [];

    /// <summary>Gets the money still owed for the month, per dog — who owes and for what.</summary>
    public ObservableCollection<DogSummaryRow> PendingSummaries { get; } = [];

    /// <summary>Gets what the month's unpaid services come to.</summary>
    public string PendingTotalLabel { get; private set; } = string.Empty;

    public bool HasPaidInMonth => PaidSummaries.Count > 0;

    public bool HasPendingInMonth => PendingSummaries.Count > 0;

    public bool HasNothingPaidInMonth => !HasPaidInMonth;

    public bool HasNothingPendingInMonth => !HasPendingInMonth;

    // Portfolio counters, so Perfil summarises the whole account and not just its billing.
    public string DogCountLabel { get; private set; } = string.Empty;

    public string TutorCountLabel { get; private set; } = string.Empty;

    /// <summary>Gets how many services fall in the selected month.</summary>
    public string ServiceCountLabel { get; private set; } = string.Empty;

    /// <summary>
    /// Gets how many services are unpaid across every month, not just the selected one — money
    /// still owed from March does not stop being owed because you are looking at August.
    /// </summary>
    public string PendingCountLabel { get; private set; } = string.Empty;

    /// <summary>
    /// Entering the tab: drops any open editor, then reloads.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ReloadAsync"/> because that also runs on a filter change and on
    /// any write elsewhere in the app — resetting there would close the profile editor under the
    /// user while they were typing in it. Only arriving at the screen should send it back to its
    /// reading state. The eye and the month filter are left alone: those are how the user chose
    /// to look at the screen, not half-finished edits.
    /// </remarks>
    public async Task ReopenAsync()
    {
        IsEditingProfile = false;
        ProfileMsg = string.Empty;

        ShowPasswordForm = false;
        CurrentPw = string.Empty;
        NewPw = string.Empty;
        ConfirmPw = string.Empty;
        PwMsg = string.Empty;
        PwMsgIsError = true;

        await ReloadAsync().WithSync();
    }

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        reloading = true;

        try
        {
            var petSitter = await repositoryPetSitter.GetAsync(session.CurrentPetSitterId).WithSync();
            CurrentUserName = petSitter?.Name ?? session.CurrentUserName;
            Email = petSitter?.Email ?? string.Empty;
            PixLabel = string.IsNullOrWhiteSpace(petSitter?.Pix) ? "Não informada" : petSitter.Pix;
            BirthDateLabel = petSitter is null ? string.Empty : petSitter.BirthDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            // Read before the labels are built below, so a hidden account never renders its
            // figures for a frame on the way in.
            MoneyVisible = !(petSitter?.HideMoney ?? false);

            var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
            var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
            var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

            RefreshYearOptions(services);

            var month = SelectedMonth?.Number ?? DateTime.Now.Month;
            income = await repositoryServices.GetMonthlyIncomeAsync(session.CurrentPetSitterId, SelectedYear, month).WithSync();
            monthServices = services
                .Where(s => s.Date.Year == SelectedYear && s.Date.Month == month)
                .ToArray();
            RefreshMoneyLabels();

            DogCountLabel = dogs.Length.ToString(CultureInfo.InvariantCulture);
            TutorCountLabel = tutors.Length.ToString(CultureInfo.InvariantCulture);
            ServiceCountLabel = services
                .Count(s => s.Date.Year == SelectedYear && s.Date.Month == month)
                .ToString(CultureInfo.InvariantCulture);
            PendingCountLabel = services.Count(s => !s.ServicePaid).ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            reloading = false;
        }
    }

    protected override async Task OnRunStarting(Unit input) => await ReopenAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        PropertyChanged -= ReloadWhenPeriodChanges;
        return Task.CompletedTask;
    }

    private void ReloadWhenPeriodChanges(object? sender, PropertyChangedEventArgs e)
    {
        if (reloading || (e.PropertyName != nameof(SelectedMonth) && e.PropertyName != nameof(SelectedYear)))
        {
            return;
        }

        AppSession.FireAndForget(ReloadAsync());
    }

    /// <summary>Formats an amount, or bullets it out while the eye is closed.</summary>
    private string Money(decimal value) => MoneyVisible ? AppSession.Money(value) : HiddenMoney;

    private void RefreshMoneyLabels()
    {
        MonthTotalLabel = Money(income.Total);

        IncomeBreakdown.Clear();
        IncomeBreakdown.Add(new IncomeRow("Passeio", Money(income.Walk)));
        IncomeBreakdown.Add(new IncomeRow("Pet sitting", Money(income.Sitting)));
        IncomeBreakdown.Add(new IncomeRow("Hotel", Money(income.Hotel)));
        IncomeBreakdown.Add(new IncomeRow("Day-Care", Money(income.DayCare)));

        RefreshMonthDetail();
    }

    /// <summary>
    /// Splits the month into what was received and what is still owed, each grouped by dog so an
    /// unpaid figure names the dog and the services behind it.
    /// </summary>
    private void RefreshMonthDetail()
    {
        var paid = monthServices.Where(s => s.ServicePaid).ToArray();
        var pending = monthServices.Where(s => !s.ServicePaid).ToArray();

        PaidSummaries.Clear();
        foreach (var row in DogSummaryBuilder.Build(paid, Money))
        {
            PaidSummaries.Add(row);
        }

        PendingSummaries.Clear();
        foreach (var row in DogSummaryBuilder.Build(pending, Money))
        {
            PendingSummaries.Add(row);
        }

        PendingTotalLabel = Money(pending.Sum(DogSummaryBuilder.AmountOf));

        // ObservableCollection.Count is not something Fody watches, so the emptiness flags derived
        // from these lists have to be announced by hand.
        OnPropertyChanged(nameof(HasPaidInMonth));
        OnPropertyChanged(nameof(HasPendingInMonth));
        OnPropertyChanged(nameof(HasNothingPaidInMonth));
        OnPropertyChanged(nameof(HasNothingPendingInMonth));
    }

    /// <summary>
    /// Keeps the year picker to years that actually have bookings, plus the current one so a
    /// brand new account still has something to select.
    /// </summary>
    /// <remarks>
    /// Synced in place rather than cleared and refilled. Clearing makes the ComboBox drop its
    /// selection, and putting the same int back afterwards raises no PropertyChanged — Fody skips
    /// an assignment that does not change the value — so the picker came back blank instead of
    /// showing the current year.
    /// </remarks>
    private void RefreshYearOptions(ServiceItem[] services)
    {
        var years = services
            .Select(s => s.Date.Year)
            .Append(DateTime.Now.Year)
            .Append(SelectedYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToArray();

        if (YearOptions.SequenceEqual(years))
        {
            return;
        }

        // SelectedYear is always one of `years`, so the selected entry is never the one removed.
        foreach (var stale in YearOptions.Where(year => !years.Contains(year)).ToArray())
        {
            YearOptions.Remove(stale);
        }

        for (var i = 0; i < years.Length; i++)
        {
            if (i >= YearOptions.Count)
            {
                YearOptions.Add(years[i]);
            }
            else if (YearOptions[i] != years[i])
            {
                YearOptions.Insert(i, years[i]);
            }
        }

        // On the very first load the item did not exist when the binding first ran, so the
        // ComboBox has nothing selected. Re-announcing the unchanged value is what makes it
        // resolve the entry now that the list has one.
        OnPropertyChanged(nameof(SelectedYear));
    }

    private async Task ToggleMoneyVisible()
    {
        MoneyVisible = !MoneyVisible;
        RefreshMoneyLabels();
        await repositoryPetSitter.SetHideMoneyAsync(session.CurrentPetSitterId, !MoneyVisible).WithSync();
    }

    private void StartEditProfile()
    {
        // Seeded from the loaded record, so cancelling and reopening starts from the saved values.
        EditName = CurrentUserName;
        EditPix = PixLabel == "Não informada" ? string.Empty : PixLabel;
        EditBirthDate = DateTime.TryParseExact(BirthDateLabel, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate)
            ? birthDate
            : DateTime.Now.Date;
        ProfileMsg = string.Empty;
        IsEditingProfile = true;
    }

    private void CancelEditProfile()
    {
        ProfileMsg = string.Empty;
        IsEditingProfile = false;
    }

    private async Task SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            ProfileMsg = "Informe o nome.";
            return;
        }

        var current = await repositoryPetSitter.GetAsync(session.CurrentPetSitterId).WithSync();
        if (current == null)
        {
            ProfileMsg = "Não foi possível carregar a conta.";
            return;
        }

        // The e-mail is carried over from the stored row, never from the form: it is the login,
        // so the profile screen shows it but does not offer to change it.
        var updated = new PetSitter
        {
            PetSitterId = current.PetSitterId,
            Email = current.Email,
            PasswordHash = current.PasswordHash,
            Name = EditName.Trim(),
            BirthDate = EditBirthDate.Date,
            Pix = string.IsNullOrWhiteSpace(EditPix) ? null : EditPix.Trim(),
        };

        var result = await repositoryPetSitter.Update(updated).WithSync();

        if (result != Response.Successful)
        {
            ProfileMsg = "Não foi possível salvar o perfil.";
            return;
        }

        // The greeting on every other screen comes from the session, so it has to hear about
        // a rename too.
        session.SignIn(updated);

        IsEditingProfile = false;
        await ReloadAsync().WithSync();
    }

    private void OpenPasswordForm()
    {
        CurrentPw = string.Empty;
        NewPw = string.Empty;
        ConfirmPw = string.Empty;
        PwMsg = string.Empty;
        PwMsgIsError = true;
        ShowPasswordForm = true;
    }

    private async Task SavePassword()
    {
        PwMsgIsError = true;

        if (string.IsNullOrEmpty(CurrentPw))
        {
            PwMsg = "Informe a senha atual.";
            return;
        }

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

        if (NewPw == CurrentPw)
        {
            PwMsg = "A nova senha é igual à atual.";
            return;
        }

        var result = await repositoryPetSitter.ChangePasswordAsync(session.CurrentPetSitterId, CurrentPw, NewPw).WithSync();

        if (result == Response.WrongPassword)
        {
            PwMsg = "Senha atual incorreta.";
            return;
        }

        if (result != Response.Successful)
        {
            PwMsg = "Não foi possível alterar a senha.";
            return;
        }

        // Cleared rather than left sitting in memory behind a closed form.
        CurrentPw = string.Empty;
        NewPw = string.Empty;
        ConfirmPw = string.Empty;
        PwMsgIsError = false;
        PwMsg = "Senha alterada.";
        ShowPasswordForm = false;
    }
}