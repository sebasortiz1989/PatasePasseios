using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Reports;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class UsersViewModel : PresentationModelBase<Unit, Unit>, PeriodScope
{
    /// <summary>What a hidden amount reads as. Wide enough not to hint at the figure's length.</summary>
    private const string HiddenMoney = "••••••";

    /// <summary>Shown in place of a Pix key that has not been set. Also how the editor spots one.</summary>
    private const string NoPix = "Não informada";

    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly AppSession session;
    private readonly ImagePicker imagePicker;
    private readonly BackupArchive backupArchive;
    private readonly CloudBackupService cloudBackup;
    private readonly CurrentView currentView;
    private readonly PresenterBase<SettingsViewModel, Unit, Unit> settingsView;
    private readonly FileExportDialog fileExportDialog;
    private readonly EventHandler dataChangedHandler;

    /// <summary>
    /// The photo file name currently in the database, as opposed to <see cref="PhotoFileName"/>
    /// which is what the open editor would save. The two differ while a new photo has been picked
    /// but not yet saved, which is what lets Cancel put the old one back.
    /// </summary>
    private string storedPhotoFileName = string.Empty;

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
        ReportExporter reportExporter,
        ShareSheet shareSheet,
        AppSession session,
        BackupArchive backupArchive,
        CloudBackupService cloudBackup,
        CurrentView currentView,
        Factory<PresenterBase<SettingsViewModel, Unit, Unit>> settingsFactory,
        FileExportDialog fileExportDialog,
        ImagePicker imagePicker)
    {
        ArgumentNullException.ThrowIfNull(settingsFactory);

        this.imagePicker = imagePicker;
        this.repositoryServices = repositoryServices;
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryPetSitter = repositoryPetSitter;
        Preview = new ReportPreview(reportExporter, shareSheet);
        this.session = session;
        this.backupArchive = backupArchive;
        this.cloudBackup = cloudBackup;
        this.currentView = currentView;
        settingsView = settingsFactory.Create();
        this.fileExportDialog = fileExportDialog;

        // Billing totals depend on services marked paid elsewhere (Agenda, service detail).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        OpenPasswordFormCommand = new SynchronizedCommand(OpenPasswordForm, SynchronizationBehavior.Discard, true);
        CancelPasswordFormCommand = new SynchronizedCommand(() => ShowPasswordForm = false, SynchronizationBehavior.Discard, true);
        SavePasswordCommand = new SynchronizedCommand(SavePassword, SynchronizationBehavior.Discard, true);
        LogoutCommand = new SynchronizedCommand(session.RequestLogout, SynchronizationBehavior.Discard, true);
        EditProfileCommand = new SynchronizedCommand(StartEditProfile, SynchronizationBehavior.Discard, true);
        ChoosePhotoCommand = new SynchronizedCommand(ChoosePhoto, SynchronizationBehavior.Discard, true);
        RemovePhotoCommand = new SynchronizedCommand(RemovePhoto, SynchronizationBehavior.Discard, true);
        OpenPhotoCommand = new SynchronizedCommand(() => ViewingPhoto = HasPhoto, SynchronizationBehavior.Discard, true);
        ClosePhotoCommand = new SynchronizedCommand(() => ViewingPhoto = false, SynchronizationBehavior.Discard, true);
        CancelProfileCommand = new SynchronizedCommand(CancelEditProfile, SynchronizationBehavior.Discard, true);
        SaveProfileCommand = new SynchronizedCommand(SaveProfile, SynchronizationBehavior.Discard, true);
        ToggleMoneyVisibleCommand = new SynchronizedCommand(ToggleMoneyVisible, SynchronizationBehavior.Discard, true);
        ExportSummaryCommand = new SynchronizedCommand(ExportSummary, SynchronizationBehavior.Discard, true);
        PreviousPeriodCommand = new SynchronizedCommand(() => StepPeriod(-1), SynchronizationBehavior.Discard, true);
        NextPeriodCommand = new SynchronizedCommand(() => StepPeriod(1), SynchronizationBehavior.Discard, true);

        // The abbreviations come from the app, not the culture: the framework default is the
        // culture's, which are not the three lower-case letters this design lays out four to a row.
        Picker = new PeriodPicker(this, ServicePeriod.ShortMonthName);
        ImportBackupCommand = new SynchronizedCommand(ImportBackup, SynchronizationBehavior.Discard, true);
        SendCloudBackupCommand = new SynchronizedCommand(SendCloudBackup, SynchronizationBehavior.Discard, true);
        SetUpCloudBackupCommand = new SynchronizedCommand(SetUpCloudBackup, SynchronizationBehavior.Discard, true);
        OpenSettingsCommand = new SynchronizedCommand(() => currentView.Show(settingsView, "Ajustes"), SynchronizationBehavior.Discard, true);
        DismissInvalidBackupCommand = new SynchronizedCommand(() => ShowInvalidBackupAlert = false, SynchronizationBehavior.Discard, true);
        DismissBackupDoneCommand = new SynchronizedCommand(() => ShowBackupDoneAlert = false, SynchronizationBehavior.Discard, true);

        // "Ano todo" first, then the twelve months — the same list TutorDetail and DogDetail pick
        // their period from, so a sitter learns one convention across every billing screen.
        foreach (var month in ServicePeriod.Months())
        {
            MonthOptions.Add(month);
        }

        var now = DateTime.Now;
        SelectedMonth = MonthOptions.First(m => m.Number == now.Month);
        SelectedYear = now.Year;
        Picker.Refresh();

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

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    /// <summary>Gets shows the profile photo full screen, at the resolution it was stored at.</summary>
    public ICommand OpenPhotoCommand { get; }

    public ICommand ClosePhotoCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the photo is open full screen. The viewer is the one place
    /// that decodes the file at its stored size, so this staying false is what keeps the tab cheap.
    /// </summary>
    public bool ViewingPhoto { get; private set; }

    /// <summary>Gets the photo file name the editor would save; also what the read view shows.</summary>
    public string PhotoFileName { get; private set; } = string.Empty;

    /// <summary>Gets the photo's full path, or null when the account has none.</summary>
    public string? PhotoPath => DogImageStore.ResolvePath(PhotoFileName);

    public bool HasPhoto => PhotoPath != null;

    public bool NoPhoto => !HasPhoto;

    /// <summary>Gets the sitter's initials, shown in place of a missing photo.</summary>
    public string Initials => AppSession.Initials(CurrentUserName);

    public ICommand ToggleMoneyVisibleCommand { get; }

    /// <summary>Gets saves the selected month's billing as an image.</summary>
    public ICommand ExportSummaryCommand { get; }

    public ICommand ImportBackupCommand { get; }

    public ICommand SendCloudBackupCommand { get; }

    /// <summary>Gets the command that picks the folder automatic backups go to.</summary>
    public ICommand SetUpCloudBackupCommand { get; }

    /// <summary>Gets the command opening Ajustes, which is pushed rather than shown inline.</summary>
    public ICommand OpenSettingsCommand { get; }

    public ICommand DismissInvalidBackupCommand { get; }

    /// <summary>Gets the command closing the "backup saved" confirmation.</summary>
    public ICommand DismissBackupDoneCommand { get; }

    /// <summary>
    /// Gets the "replace the file already there?" question, asked mid-export on the platforms where
    /// the app names the file itself. Bound to its own dialog in the view.
    /// </summary>
    public ConfirmRequest ReplaceRequest { get; } = new();

    /// <summary>
    /// Gets the rendered month summary, held on screen with Compartilhar and Salvar beside it.
    /// </summary>
    public ReportPreview Preview { get; }

    /// <summary>
    /// Gets a value indicating whether the "not a valid backup" alert is up. A popup rather than
    /// the inline message for this one case: it is the only outcome where the user picked a file
    /// and nothing happened, so it has to be impossible to miss.
    /// </summary>
    public bool ShowInvalidBackupAlert { get; private set; }

    /// <summary>Gets a value indicating whether the "backup saved" confirmation is up.</summary>
    public bool ShowBackupDoneAlert { get; private set; }

    /// <summary>Gets where the backup went and when, e.g. "Salvo em "Documents/Patas" em 21/08/2026 às 14:30.".</summary>
    public string BackupDoneMessage { get; private set; } = string.Empty;

    /// <summary>Gets the title of the alert shown when a backup could not be imported.</summary>
    public string InvalidBackupTitle { get; private set; } = string.Empty;

    /// <summary>Gets the body of that alert, which differs by why the import was refused.</summary>
    public string InvalidBackupMessage { get; private set; } = string.Empty;

    /// <summary>Gets the confirmation left after a summary image is written, or empty.</summary>
    public string SummaryMsg { get; private set; } = string.Empty;

    public bool HasSummaryMsg => !string.IsNullOrEmpty(SummaryMsg);

    /// <summary>Gets the outcome of the last export or import, shown under the two buttons.</summary>
    public string BackupMsg { get; private set; } = string.Empty;

    public bool HasBackupMsg => !string.IsNullOrEmpty(BackupMsg);

    public bool BackupMsgIsError { get; private set; }

    /// <summary>Gets when the automatic backup last ran, as the sitter reads it.</summary>
    public string CloudBackupLabel { get; private set; } = string.Empty;

    /// <summary>Gets the automatic-backup row's title, which changes once a folder is chosen.</summary>
    public string CloudBackupTitle { get; private set; } = "Ativar backup automático";

    /// <summary>
    /// Gets a value indicating whether a destination folder is set up and reachable. "Enviar agora"
    /// only exists once it is — before that there is nowhere to send to, and the row would fail
    /// every time it was pressed.
    /// </summary>
    public bool CloudBackupLinked { get; private set; }

    /// <summary>Gets the app version, shown at the foot of the profile screen.</summary>
    public string VersionLabel => AppVersion.Label;

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

    /// <summary>Gets the period as one line, e.g. "Agosto 2026".</summary>
    public string PeriodLabel => ServicePeriod.Label(SelectedMonth, SelectedYear);

    /// <summary>Gets the command stepping the period back one month.</summary>
    public ICommand PreviousPeriodCommand { get; private set; } = null!;

    /// <summary>Gets the command stepping the period forward one month.</summary>
    public ICommand NextPeriodCommand { get; private set; } = null!;

    /// <summary>Gets the inline period picker: a year row over a grid of months.</summary>
    public PeriodPicker Picker { get; }

    public MonthOption? SelectedMonth { get; set; }

    public int SelectedYear { get; set; }

    /// <summary>
    /// Gets a value indicating whether the "Ano todo" entry is selected rather than one month —
    /// what the labels around the billing figures switch on, since the same pickers now scope
    /// either a month or the whole year.
    /// </summary>
    public bool IsWholeYearPeriod => SelectedMonth is null or { Number: ServicePeriod.WholeYear };

    public string ReceivedHeading => IsWholeYearPeriod ? "RECEBIDO NO ANO" : "RECEBIDO NO MÊS";

    public string PendingHeading => IsWholeYearPeriod ? "A RECEBER NO ANO" : "A RECEBER NO MÊS";

    public string ServiceCountKicker => IsWholeYearPeriod ? "No ano" : "No mês";

    public string NoPaidLabel => IsWholeYearPeriod ? "Nada recebido neste ano." : "Nada recebido neste mês.";

    public string NoPendingLabel => IsWholeYearPeriod ? "Nada pendente neste ano." : "Nada pendente neste mês.";

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
        SummaryMsg = string.Empty;

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
            PixLabel = string.IsNullOrWhiteSpace(petSitter?.Pix) ? NoPix : petSitter.Pix;
            storedPhotoFileName = petSitter?.Image ?? string.Empty;
            PhotoFileName = storedPhotoFileName;
            BirthDateLabel = petSitter is null ? string.Empty : petSitter.BirthDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            // Read before the labels are built below, so a hidden account never renders its
            // figures for a frame on the way in.
            MoneyVisible = !(petSitter?.HideMoney ?? false);

            var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
            var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
            var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

            RefreshYearOptions(services);

            // month is the WholeYear sentinel (0) when "Ano todo" is picked — the repository call
            // and ServicePeriod.Matches both treat that as "any month", so the totals below become
            // the year's rather than a single month's without a separate code path.
            var month = SelectedMonth?.Number ?? DateTime.Now.Month;
            income = await repositoryServices.GetMonthlyIncomeAsync(session.CurrentPetSitterId, SelectedYear, month).WithSync();
            monthServices = services
                .Where(s => ServicePeriod.Matches(s, SelectedMonth, SelectedYear))
                .ToArray();
            RefreshMoneyLabels();

            DogCountLabel = dogs.Length.ToString(CultureInfo.InvariantCulture);
            TutorCountLabel = tutors.Length.ToString(CultureInfo.InvariantCulture);
            ServiceCountLabel = monthServices.Length.ToString(CultureInfo.InvariantCulture);

            // Chargeable rather than merely unpaid: an unexecuted booking is not money owed.
            PendingCountLabel = services.Count(s => s.AmountDue > 0m).ToString(CultureInfo.InvariantCulture);

            await RefreshCloudBackupLabelAsync().WithSync();
        }
        finally
        {
            reloading = false;
        }
    }

    /// <summary>
    /// Never runs: tabs are shown by assigning CurrentView, not pushed, so the run lifecycle
    /// does not reach them. The load happens in the view's OnLoaded, and sign-out cleanup in
    /// AppSession.SignOut. Present only because the base declares it abstract.
    /// </summary>
    protected override Task OnRunStarting(Unit input) => Task.CompletedTask;

    private void ReloadWhenPeriodChanges(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SelectedMonth) && e.PropertyName != nameof(SelectedYear))
        {
            return;
        }

        // Unguarded: the picker's highlight and its whole-year label must follow the period even
        // while a reload is already under way.
        Picker?.Refresh();

        if (reloading)
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
        IncomeBreakdown.Add(new IncomeRow("Day Care", Money(income.DayCare)));

        RefreshMonthDetail();
    }

    /// <summary>
    /// Splits the month into what was received and what is still owed, each grouped by dog so an
    /// unpaid figure names the dog and the services behind it.
    /// </summary>
    private void RefreshMonthDetail()
    {
        var paid = monthServices.Where(s => s.ServicePaid).ToArray();

        // Only work already carried out is owed for. A booking still to do belongs to neither
        // column: it has not been paid, but it cannot be billed either.
        var pending = monthServices.Where(s => s.AmountDue > 0m).ToArray();

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

    /// <summary>
    /// Writes the selected period's billing to an image: what came in by type, and every service
    /// with whether it has been paid.
    /// </summary>
    /// <remarks>
    /// The eye is not consulted. Hiding amounts on screen is about who is standing next to you,
    /// and a summary of bulleted-out figures would be useless. The period is whatever the pickers
    /// above are scoped to — the whole year for the "Ano todo" entry, one month otherwise — and
    /// <see cref="monthServices"/> and <see cref="income"/> are already filtered to it by
    /// <see cref="ReloadAsync"/>, so this only has to name it.
    /// </remarks>
    private async Task ExportSummary()
    {
        var isWholeYear = IsWholeYearPeriod;
        var monthName = SelectedMonth?.Label ?? string.Empty;
        var year = SelectedYear.ToString(CultureInfo.InvariantCulture);
        var periodPhrase = isWholeYear ? "no ano" : "no mês";
        var periodDemonstrative = isWholeYear ? "neste ano" : "neste mês";

        var report = new ReportDocument
        {
            Title = "Faturamento",
            Subtitle = $"{monthName} de {year}",
            Footer = $"Gerado em {DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture)}",
        };

        // No Pix key here: this report is the sitter's own record of the period, not something a
        // tutor is asked to pay against. The tutor summary is where the key belongs.
        report.Summary.Add(new ReportField("Pet sitter", CurrentUserName));

        var received = new ReportSection { Heading = $"Recebido {periodPhrase}" };
        received.Columns.Add("Tipo");
        received.Columns.Add("Valor");
        received.RightAligned.Add(false);
        received.RightAligned.Add(true);
        received.Rows.Add(new ReportRow("Passeio", AppSession.Money(income.Walk)));
        received.Rows.Add(new ReportRow("Pet sitting", AppSession.Money(income.Sitting)));
        received.Rows.Add(new ReportRow("Hotel", AppSession.Money(income.Hotel)));
        received.Rows.Add(new ReportRow("Day Care", AppSession.Money(income.DayCare)));
        received.Totals.Add(new ReportField("Total recebido", AppSession.Money(income.Total), true));
        report.Sections.Add(received);

        var services = new ReportSection
        {
            Heading = isWholeYear ? "Serviços do ano" : "Serviços do mês",
            EmptyMessage = $"Nenhum serviço {periodDemonstrative}.",
        };

        // Grouped rather than listed one booking per line: a busy month runs to hundreds of rows,
        // and the sitter reads this to see who brought in what, not to audit individual walks.
        foreach (var column in new[] { "Cachorro", "Tipo", "Qtd.", "Recebido", "A receber" })
        {
            services.Columns.Add(column);
        }

        foreach (var aligned in new[] { false, false, true, true, true })
        {
            services.RightAligned.Add(aligned);
        }

        var grouped = monthServices
            .GroupBy(s => (s.DogName, s.Kind))
            .OrderBy(g => g.Key.DogName, StringComparer.CurrentCulture)
            .ThenBy(g => g.Key.Kind);

        foreach (var group in grouped)
        {
            // Received is money actually in; AmountDue is what may still be asked for, which is
            // zero for anything unexecuted — the charging rule, not just "unpaid".
            var receivedFromGroup = group.Where(s => s.ServicePaid).Sum(s => s.Total);
            var owedFromGroup = group.Sum(s => s.AmountDue);

            services.Rows.Add(new ReportRow(
                group.Key.DogName,
                AppSession.TypeLabel(group.Key.Kind),
                group.Count().ToString(CultureInfo.InvariantCulture),
                AppSession.Money(receivedFromGroup),
                AppSession.Money(owedFromGroup)));
        }

        // Only executed work may be billed, so the receivable is what has actually been carried
        // out and not yet paid for. Work still to come is reported separately rather than folded
        // in, so the figure is never one the sitter cannot ask for.
        var chargeable = monthServices.Sum(s => s.AmountDue);
        var upcoming = monthServices.Sum(s => s.AmountUpcoming);

        services.Totals.Add(new ReportField($"Serviços {periodPhrase}", monthServices.Length.ToString(CultureInfo.InvariantCulture)));

        if (upcoming > 0m)
        {
            services.Totals.Add(new ReportField("A executar (ainda não cobrado)", AppSession.Money(upcoming)));
        }

        services.Totals.Add(new ReportField($"A receber {periodPhrase}", AppSession.Money(chargeable), true));
        report.Sections.Add(services);

        var periodSlug = isWholeYear
            ? year
            : $"{year}-{(SelectedMonth?.Number ?? 1).ToString("00", CultureInfo.InvariantCulture)}";

        // Shown rather than saved: the preview offers Compartilhar and Salvar side by side, so
        // sending the month to someone no longer means writing a file first and going to find it.
        var shown = await Preview
            .ShowAsync(report, $"faturamento-{periodSlug}", AskReplaceAsync)
            .WithSync();

        SummaryMsg = shown == Response.Successful ? string.Empty : "Não foi possível gerar o resumo.";
    }

    /// <summary>
    /// Answers the export's "there is already one of these" question through the screen's dialog.
    /// </summary>
    private Task<bool> AskReplaceAsync(string fileName) =>
        ReplaceRequest.AskAsync($"Já existe um arquivo chamado {fileName} nesta pasta. Substituir?");

    /// <summary>
    /// Sends a backup to the automatic destination now, rather than waiting for tomorrow's run.
    /// </summary>
    /// <remarks>
    /// Reports its outcome, unlike the daily run: this one the sitter started, so silence would
    /// read as nothing having happened.
    /// </remarks>
    private async Task SendCloudBackup()
    {
        BackupMsgIsError = false;
        BackupMsg = "Enviando backup…";

        var result = await cloudBackup.RunAsync().WithSync();
        var destination = await cloudBackup.DestinationNameAsync().WithSync();

        BackupMsgIsError = result != Response.Successful;
        BackupMsg = result == Response.Successful
            ? $"Backup enviado para \"{destination}\"."
            : "Não foi possível enviar o backup.";

        // A backup is the one action here with nothing on screen to show for it — the row's caption
        // changes, but a sitter who just tapped it is owed a plain answer that it worked, and when.
        if (result == Response.Successful)
        {
            var stamp = DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture);
            BackupDoneMessage = $"Salvo em \"{destination}\" em {stamp}.";
            ShowBackupDoneAlert = true;
        }

        await RefreshCloudBackupLabelAsync().WithSync();
    }

    /// <summary>
    /// Chooses the folder automatic backups go to, and sends the first one straight away.
    /// </summary>
    /// <remarks>
    /// The one setup step, and the whole point of the row: after this the daily run has somewhere
    /// to write. The first backup runs immediately rather than waiting for tomorrow morning, both
    /// because the sitter has just asked for this and because it proves the folder is actually
    /// writable while they are still looking at the screen.
    /// </remarks>
    private async Task SetUpCloudBackup()
    {
        BackupMsgIsError = false;
        BackupMsg = string.Empty;

        if (await cloudBackup.LinkAsync().WithSync() != Response.Successful)
        {
            // Cancelling the folder picker is the ordinary case, not an error worth shouting
            // about. Only refresh, so the row goes back to saying what it said before.
            await RefreshCloudBackupLabelAsync().WithSync();
            return;
        }

        await RefreshCloudBackupLabelAsync().WithSync();
        await SendCloudBackup().WithSync();
    }

    private async Task RefreshCloudBackupLabelAsync()
    {
        var destination = await cloudBackup.DestinationNameAsync().WithSync();

        CloudBackupLinked = destination is { Length: > 0 };

        if (!CloudBackupLinked)
        {
            CloudBackupTitle = "Ativar backup automático";
            CloudBackupLabel = "Escolha uma pasta — no Drive ou no aparelho. Depois disso o app envia uma cópia todo dia de manhã, sozinho.";
            return;
        }

        var last = await cloudBackup.LastUploadAsync().WithSync();

        CloudBackupTitle = "Backup automático";
        CloudBackupLabel = last == null
            ? $"Salvando em \"{destination}\". Nenhuma cópia enviada ainda. Toque para trocar de pasta."
            : $"Salvando em \"{destination}\". Última cópia em {last.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}. Toque para trocar de pasta.";
    }

    /// <summary>
    /// Replaces everything on this device with the chosen backup, then signs out. The session is
    /// holding the id of an account from the old database, which the restored one may not have —
    /// logging in again is what re-establishes who the user is.
    /// </summary>
    private async Task ImportBackup()
    {
        BackupMsgIsError = false;
        BackupMsg = string.Empty;

        var source = await fileExportDialog.OpenBackupAsync().WithSync();
        if (source == null)
        {
            return;
        }

        Response result;
        await using (source.ConfigureAwait(true))
        {
            result = await backupArchive.RestoreFromAsync(source).WithSync();
        }

        if (result != Response.Successful)
        {
            // The inline message is left blank: the popup is the whole story, and a second copy of
            // it sitting under the buttons afterwards reads like a lingering failure.
            RejectBackup(result);
            return;
        }

        BackupMsg = "Backup importado. Entre novamente.";
        session.NotifyDataChanged();
        session.RequestLogout();
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
        EditPix = PixLabel == NoPix ? string.Empty : PixLabel;
        EditBirthDate = DateTime.TryParseExact(BirthDateLabel, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate)
            ? birthDate
            : DateTime.Now.Date;
        PhotoFileName = storedPhotoFileName;
        ProfileMsg = string.Empty;
        IsEditingProfile = true;

        // The viewer opens from the reading state's portrait, which the editor replaces. Leaving it
        // open would strand a full-screen photo over a form the user can no longer reach.
        ViewingPhoto = false;
    }

    private void CancelEditProfile()
    {
        DiscardUnsavedPhoto();
        ProfileMsg = string.Empty;
        IsEditingProfile = false;
    }

    private async Task ChoosePhoto()
    {
        using var picked = await imagePicker.PickAsync().WithSync();
        if (picked == null)
        {
            return;
        }

        DiscardUnsavedPhoto();
        PhotoFileName = await DogImageStore.SaveAsync(picked.Content, picked.Extension).WithSync();
    }

    private Task RemovePhoto()
    {
        DiscardUnsavedPhoto();
        PhotoFileName = string.Empty;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Drops a photo that was picked but never saved. The stored one is left alone — it is still
    /// what the database points at until Save says otherwise.
    /// </summary>
    private void DiscardUnsavedPhoto()
    {
        if (PhotoFileName != storedPhotoFileName)
        {
            DogImageStore.Delete(PhotoFileName);
        }

        PhotoFileName = storedPhotoFileName;
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
            Image = string.IsNullOrEmpty(PhotoFileName) ? null : PhotoFileName,
        };

        var result = await repositoryPetSitter.Update(updated).WithSync();

        if (result != Response.Successful)
        {
            ProfileMsg = "Não foi possível salvar o perfil.";
            return;
        }

        // Only now is the replaced photo unreferenced, so this is where it can go.
        if (storedPhotoFileName != PhotoFileName)
        {
            DogImageStore.Delete(storedPhotoFileName);
            storedPhotoFileName = PhotoFileName;
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

    /// <summary>
    /// Puts up the alert explaining why an import was refused.
    /// </summary>
    /// <remarks>
    /// A version mismatch is told apart from an unrelated file on purpose. It <i>is</i> the user's
    /// backup, written by this app, and telling them it is not risks them deleting the only copy
    /// they have. What it needs is a build that understands it, so that is what the message asks
    /// for.
    /// </remarks>
    private void RejectBackup(Response result)
    {
        var incompatible = result == Response.IncompatibleVersion;

        InvalidBackupTitle = incompatible ? "Backup de outra versão" : "Backup inválido";
        InvalidBackupMessage = incompatible
            ? "Este backup foi criado por outra versão do Patas & Passeios e não pode ser restaurado aqui. Atualize o aplicativo e tente de novo. Nada foi alterado neste aparelho."
            : "Este arquivo não é um backup do Patas & Passeios. Escolha um .zip exportado por este aplicativo. Nada foi alterado neste aparelho.";

        ShowInvalidBackupAlert = true;
    }

    /// <summary>
    /// Moves the billing period, replacing the two drop-downs this screen used to carry.
    /// </summary>
    /// <param name="delta">−1 or +1.</param>
    private void StepPeriod(int delta)
    {
        var (month, year) = ServicePeriod.Step(SelectedMonth, SelectedYear, delta);

        if (!YearOptions.Contains(year))
        {
            YearOptions.Add(year);
        }

        // Guarded pair, then one reload: each assignment fires its own change hook, so a step
        // across a year boundary used to start two full reloads racing each other.
        reloading = true;
        try
        {
            SelectedYear = year;
            SelectedMonth = MonthOptions.FirstOrDefault(m => m.Number == month) ?? SelectedMonth;
        }
        finally
        {
            reloading = false;
        }

        AppSession.FireAndForget(ReloadAsync());
    }
}