using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Reports;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryPetSitter repositoryPetSitter;
    private readonly ReportExporter reportExporter;
    private readonly AppSession session;
    private readonly CurrentView currentView;

    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;

    /// <summary>
    /// Every service for this tutor's dogs, past and future. Held because a payment is settled
    /// against the whole history, not just what is still to come.
    /// </summary>
    private ServiceItem[] tutorServices = [];

    /// <summary>
    /// Guards the picker hooks while <see cref="ReloadAsync"/> rebuilds the year list. Assigning
    /// SelectedYear there would otherwise re-enter the reload through Fody's OnXChanged hook.
    /// </summary>
    private bool rebuildingOptions;

    public TutorDetailViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        RepositoryServices repositoryServices,
        RepositoryPetSitter repositoryPetSitter,
        ReportExporter reportExporter,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.repositoryPetSitter = repositoryPetSitter;
        this.reportExporter = reportExporter;
        this.session = session;
        this.currentView = currentView;
        serviceDetailView = serviceDetailFactory.Create();
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
        OpenPaymentCommand = new SynchronizedCommand(OpenPayment, SynchronizationBehavior.Discard, true);
        CancelPaymentCommand = new SynchronizedCommand(CancelPayment, SynchronizationBehavior.Discard, true);
        ConfirmPaymentCommand = new SynchronizedCommand(ConfirmPayment, SynchronizationBehavior.Discard, true);
        ExportCommand = new SynchronizedCommand(Export, SynchronizationBehavior.Discard, true);

        foreach (var month in ServicePeriod.Months())
        {
            MonthOptions.Add(month);
        }

        // "Ano todo" by default so the bill still opens showing everything owed this year, rather
        // than only what falls in the current month.
        SelectedMonth = MonthOptions[0];
        SelectedYear = DateTime.Now.Year;
    }

    public ICommand BackCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    public ICommand OpenPaymentCommand { get; }

    public ICommand CancelPaymentCommand { get; }

    public ICommand ConfirmPaymentCommand { get; }

    public ICommand ExportCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the screen is in edit mode — see
    /// <see cref="DogDetailViewModel.IsEditing"/> for why the editor replaces the fields in place
    /// rather than opening a separate form.
    /// </summary>
    public bool IsEditing { get; private set; }

    public bool IsViewing => !IsEditing;

    public string Initials { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Neighborhood { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string EditName { get; set; } = string.Empty;

    public string EditPhone { get; set; } = string.Empty;

    public string EditAddress { get; set; } = string.Empty;

    public string EditError { get; private set; } = string.Empty;

    public bool HasEditError => !string.IsNullOrEmpty(EditError);

    public string DogNames { get; private set; } = string.Empty;

    /// <summary>
    /// Gets what may be billed today: work already carried out and not yet paid for, across every
    /// month. Booked-but-unexecuted services are not in it — see <see cref="UpcomingTotalLabel"/>.
    /// </summary>
    public string TotalDueLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether there is anything to collect, so the button can hide when there is not.</summary>
    public bool HasBalance { get; private set; }

    /// <summary>Gets what the tutor's booked-but-not-yet-carried-out work will come to once it is done.</summary>
    public string UpcomingTotalLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether there is unexecuted work worth showing a figure for.</summary>
    public bool HasUpcoming { get; private set; }

    /// <summary>
    /// Gets what is owed back to the tutor: money they handed over beyond their balance. Spent
    /// automatically against the next service booked for one of their dogs.
    /// </summary>
    public string CreditLabel { get; private set; } = string.Empty;

    public bool HasCredit { get; private set; }

    /// <summary>Gets a value indicating whether the amount-received form is open.</summary>
    public bool IsRegisteringPayment { get; private set; }

    /// <summary>The amount received, as typed.</summary>
    public string PaymentAmount { get; set; } = string.Empty;

    /// <summary>
    /// Gets a plain-language description of what confirming would do, recomputed as the amount is
    /// typed so the split is visible before it is written rather than after.
    /// </summary>
    public string PaymentPreview => BuildPaymentPreview();

    public bool HasPaymentPreview => !string.IsNullOrEmpty(PaymentPreview);

    public string PaymentError { get; private set; } = string.Empty;

    public bool HasPaymentError => !string.IsNullOrEmpty(PaymentError);

    /// <summary>Gets the confirmation left behind after a payment is recorded, or empty.</summary>
    public string PaymentMsg { get; private set; } = string.Empty;

    public bool HasPaymentMsg => !string.IsNullOrEmpty(PaymentMsg);

    /// <summary>Gets the confirmation left after an image is written, or empty.</summary>
    public string ExportMsg { get; private set; } = string.Empty;

    public bool HasExportMsg => !string.IsNullOrEmpty(ExportMsg);

    public bool NoPending { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether settled bookings are listed too. Off by default:
    /// this list is the tutor's outstanding bill.
    /// </summary>
    public bool ShowPaidServices { get; set; }

    /// <summary>Gets or sets the month the list is scoped to, or the whole-year entry.</summary>
    public MonthOption? SelectedMonth { get; set; }

    /// <summary>Gets or sets the year the list is scoped to.</summary>
    public int SelectedYear { get; set; }

    public ObservableCollection<MonthOption> MonthOptions { get; } = [];

    /// <summary>Gets the years this tutor has services in, plus the current one.</summary>
    public ObservableCollection<int> YearOptions { get; } = [];

    public ObservableCollection<TutorFutureServiceRow> PendingServices { get; } = [];

    /// <summary>
    /// Public because the View calls it from OnLoaded — see <see cref="DogDetailViewModel"/> for
    /// why OnRunStarting is not enough for a screen shown through CurrentView.
    /// </summary>
    public async Task ReloadAsync()
    {
        // Before the early returns below, so arriving here always lands on the reading state —
        // leaving mid-edit and coming back should not resume a half-finished form.
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;
        IsRegisteringPayment = false;
        PaymentError = string.Empty;
        ExportMsg = string.Empty;

        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        var tutor = await repositoryTutors.GetAsync(tutorId).WithSync();
        if (tutor == null)
        {
            return;
        }

        Initials = AppSession.Initials(tutor.Name);
        Name = tutor.Name;
        CreditLabel = AppSession.Money(tutor.Credit);
        HasCredit = tutor.Credit > 0m;
        Neighborhood = tutor.Address ?? string.Empty;
        Phone = tutor.Telephone;

        var dogs = await repositoryDogs.ListForTutorAsync(tutorId).WithSync();
        DogNames = dogs.Length == 0 ? "Nenhum cachorro cadastrado." : string.Join(", ", dogs.Select(d => d.Name));

        var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogIds = dogs.Select(d => d.DogId).ToHashSet();
        tutorServices = [.. services.Where(s => dogIds.Contains(s.DogId)).OrderBy(s => s.Date).ThenBy(s => s.ServiceId)];

        // Only executed work can be billed, which AmountDue already encodes.
        var due = tutorServices.Sum(s => s.AmountDue);
        TotalDueLabel = AppSession.Money(due);
        HasBalance = due > 0m;

        var upcoming = tutorServices.Sum(s => s.AmountUpcoming);
        UpcomingTotalLabel = AppSession.Money(upcoming);
        HasUpcoming = upcoming > 0m;

        RefreshYearOptions(tutorServices);

        // Everything unsettled, executed or not: the sitter needs to see the work still to come as
        // well as the bill. Which of the two a row is shows in its Feito / A fazer tag, and only
        // the executed ones are counted into the figure above.
        //
        // Scoped to the chosen period, and widened to settled bookings when the user asks. The
        // totals above stay whole-account on purpose — a balance owed does not shrink because the
        // list is currently showing one month.
        var pending = tutorServices
            .Where(s => ServicePeriod.Matches(s, SelectedMonth, SelectedYear))
            .Where(s => ShowPaidServices || !s.ServicePaid)
            .OrderByDescending(s => s.Date)
            .ToArray();

        ClearPendingServices();
        foreach (var service in pending)
        {
            // CA2000: ownership passes to the row, which disposes the command when the list is
            // rebuilt — see ClearPendingServices.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(
                () => Open(service.Kind, service.ServiceId),
                SynchronizationBehavior.Discard,
                true);
#pragma warning restore CA2000

            PendingServices.Add(new TutorFutureServiceRow(
                service.DogName,
                AppSession.TypeLabel(service.Kind),
                AppSession.DateTimeLabel(service.Date, service.Kind),
                service.ServicePaid,
                service.ServicePaid ? "Pago" : "Pendente",
                service.ServiceDone,
                service.ServiceDone ? "Feito" : "A fazer",
                openCommand));
        }

        NoPending = pending.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        ClearPendingServices();
        return Task.CompletedTask;
    }

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever ShowPaidServices changes.</summary>
    protected void OnShowPaidServicesChanged() => ReloadIfIdle();

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedMonth changes.</summary>
    protected void OnSelectedMonthChanged() => ReloadIfIdle();

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedYear changes.</summary>
    protected void OnSelectedYearChanged() => ReloadIfIdle();

    private static bool TryParseAmount(string text, out decimal amount) =>
        decimal.TryParse(text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private Task OpenPayment()
    {
        PaymentAmount = string.Empty;
        PaymentError = string.Empty;
        PaymentMsg = string.Empty;
        IsRegisteringPayment = true;
        return Task.CompletedTask;
    }

    private Task CancelPayment()
    {
        PaymentError = string.Empty;
        IsRegisteringPayment = false;
        return Task.CompletedTask;
    }

    private async Task ConfirmPayment()
    {
        if (!TryParseAmount(PaymentAmount, out var amount) || amount <= 0m)
        {
            PaymentError = "Informe um valor válido.";
            return;
        }

        // No payments is not a failure: a tutor may hand money over before any of the work has been
        // carried out, and the whole of it then becomes credit.
        var (payments, applied) = PaymentAllocation.Allocate(tutorServices, amount);

        if (payments.Count > 0 && await repositoryServices.RegisterPaymentAsync(payments).WithSync() != Response.Successful)
        {
            PaymentError = "Não foi possível registrar o pagamento.";
            return;
        }

        var settled = payments.Count(p => p.FullyPaid);
        var leftover = amount - applied;
        var message = applied > 0m
            ? $"Recebido {AppSession.Money(applied)} — {(settled == 1 ? "1 serviço quitado" : $"{settled} serviços quitados")}."
            : string.Empty;

        if (leftover > 0m && session.SelectedTutorId is int creditTutorId)
        {
            // More than the executed work came to, so the remainder is an advance. It is held as
            // credit in the tutor's favour and spent as each future service is carried out.
            var tutor = await repositoryTutors.GetAsync(creditTutorId).WithSync();
            await repositoryTutors.SetCreditAsync(creditTutorId, (tutor?.Credit ?? 0m) + leftover).WithSync();

            message += message.Length == 0
                ? $"Recebido {AppSession.Money(leftover)} adiantado, guardado como crédito para os próximos serviços."
                : $" {AppSession.Money(leftover)} ficam como crédito para os próximos serviços.";
        }

        PaymentError = string.Empty;
        IsRegisteringPayment = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
        PaymentMsg = message;
    }

    /// <summary>Describes what confirming the typed amount would do, without writing anything.</summary>
    private string BuildPaymentPreview()
    {
        if (!IsRegisteringPayment || !TryParseAmount(PaymentAmount, out var amount) || amount <= 0m)
        {
            return string.Empty;
        }

        var (payments, applied) = PaymentAllocation.Allocate(tutorServices, amount);
        if (payments.Count == 0)
        {
            // Nothing carried out yet to charge for, so the whole amount is an advance.
            return $"Nada executado a cobrar. {AppSession.Money(amount)} viram crédito para os próximos serviços.";
        }

        var settled = payments.Count(p => p.FullyPaid);
        var partial = payments.FirstOrDefault(p => !p.FullyPaid);
        var preview = settled == 1 ? "Quita 1 serviço" : $"Quita {settled} serviços";

        if (partial != null)
        {
            var remainder = partial.Kind == ServiceKind.Hotel
                ? partial.Price * tutorServices.First(s => s.Kind == partial.Kind && s.ServiceId == partial.ServiceId).Nights
                : partial.Price;

            preview += $" e deixa {AppSession.Money(remainder)} em aberto no seguinte.";
        }
        else
        {
            var leftover = amount - applied;
            preview += leftover > 0m
                ? $". Sobram {AppSession.Money(leftover)}, que viram crédito para os próximos serviços."
                : " — nada fica em aberto.";
        }

        return preview;
    }

    /// <summary>
    /// Opens the tapped service. CurrentView keeps a back stack, so the service screen's own Back
    /// returns here rather than to the tutors list.
    /// </summary>
    private Task Open(ServiceKind kind, int serviceId)
    {
        session.SelectedServiceKind = kind;
        session.SelectedServiceId = serviceId;
        currentView.ViewShown = serviceDetailView;
        return Task.CompletedTask;
    }

    private void ReloadIfIdle()
    {
        if (!rebuildingOptions)
        {
            AppSession.FireAndForget(ReloadAsync());
        }
    }

    /// <summary>
    /// Keeps the year picker to years this tutor has services in, plus the current one. Rebuilt in
    /// place under the guard so re-selecting the same value does not reload again.
    /// </summary>
    private void RefreshYearOptions(ServiceItem[] services)
    {
        var years = ServicePeriod.Years(services);
        if (YearOptions.SequenceEqual(years))
        {
            return;
        }

        rebuildingOptions = true;
        try
        {
            var kept = years.Contains(SelectedYear) ? SelectedYear : years[0];

            YearOptions.Clear();
            foreach (var year in years)
            {
                YearOptions.Add(year);
            }

            SelectedYear = kept;

            // The item did not exist when the binding first ran, so the ComboBox has nothing
            // selected; re-announcing the unchanged value is what makes it resolve the entry.
            OnPropertyChanged(nameof(SelectedYear));
        }
        finally
        {
            rebuildingOptions = false;
        }
    }

    private void ClearPendingServices()
    {
        foreach (var row in PendingServices)
        {
            row.Dispose();
        }

        PendingServices.Clear();
    }

    /// <summary>
    /// Writes this tutor's history to an image: every service, grouped by month, with what has
    /// been paid and what is still owed, and the Pix key to settle it with.
    /// </summary>
    /// <remarks>
    /// Months run newest first, since a bill is usually about what just happened. An image rather
    /// than a document because it is meant to be sent straight to the tutor over WhatsApp.
    /// </remarks>
    private async Task Export()
    {
        var report = new ReportDocument
        {
            Title = Name,
            Subtitle = "Serviços por mês",
            Footer = $"Gerado em {DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture)}",
        };

        report.Summary.Add(new ReportField("Telefone", Phone));
        if (!string.IsNullOrWhiteSpace(Neighborhood))
        {
            report.Summary.Add(new ReportField("Endereço", Neighborhood));
        }

        report.Summary.Add(new ReportField("Cachorros", DogNames));

        foreach (var month in tutorServices.GroupBy(s => new DateTime(s.Date.Year, s.Date.Month, 1)).OrderByDescending(g => g.Key))
        {
            var monthName = Brazil.DateTimeFormat.GetMonthName(month.Key.Month);
            var section = new ReportSection
            {
                Heading = $"{char.ToUpper(monthName[0], Brazil)}{monthName[1..]} de {month.Key.Year.ToString(CultureInfo.InvariantCulture)}",
            };

            // Execução and Pagamento are separate columns because they answer separate questions:
            // a tutor querying a bill wants to see that the work happened as well as what is owed.
            foreach (var column in new[] { "Cachorro", "Tipo", "Data", "Valor", "Execução", "Pagamento" })
            {
                section.Columns.Add(column);
            }

            foreach (var aligned in new[] { false, false, false, true, true, true })
            {
                section.RightAligned.Add(aligned);
            }

            foreach (var service in month.OrderBy(s => s.Date))
            {
                section.Rows.Add(new ReportRow(
                    service.DogName,
                    AppSession.TypeLabel(service.Kind),
                    AppSession.DateTimeLabel(service.Date, service.Kind),
                    AppSession.Money(service.Total),
                    service.ServiceDone ? "Feito" : "A fazer",
                    service.ServicePaid ? "Pago" : "Pendente"));
            }

            // Only executed work is billable, so the month's headline figure is what may actually
            // be asked for. Work still to come is listed separately rather than folded in, so the
            // tutor is never shown a total that includes services that have not happened.
            var paid = month.Where(s => s.ServicePaid).Sum(s => s.Total);
            var chargeable = month.Sum(s => s.AmountDue);
            var upcoming = month.Sum(s => s.AmountUpcoming);

            section.Totals.Add(new ReportField("Já pago", AppSession.Money(paid)));
            if (upcoming > 0m)
            {
                section.Totals.Add(new ReportField("A executar (ainda não cobrado)", AppSession.Money(upcoming)));
            }

            section.Totals.Add(new ReportField("A pagar", AppSession.Money(chargeable), true));
            report.Sections.Add(section);
        }

        if (report.Sections.Count == 0)
        {
            report.Sections.Add(new ReportSection
            {
                Heading = "Serviços",
                EmptyMessage = "Nenhum serviço registrado para este tutor.",
            });
        }

        // What the tutor is actually being asked for: executed and unpaid, nothing else.
        var chargeableTotal = tutorServices.Sum(s => s.AmountDue);
        await AddPaymentSectionAsync(report, chargeableTotal).WithSync();

        var slug = new string([.. Name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')]).Trim('-');
        var fileName = await reportExporter.ExportAsync(report, $"servicos-{slug}").WithSync();
        ExportMsg = fileName == null ? string.Empty : $"Resumo salvo: {fileName}";
    }

    /// <summary>
    /// Adds where to send the money: the pet sitter's own name and Pix key, with the outstanding
    /// amount beside them, so the tutor can pay straight from the image.
    /// </summary>
    /// <remarks>
    /// Read from the account rather than the session, because the session carries only the name —
    /// and a Pix key edited on the profile screen has to reach the next export.
    /// </remarks>
    /// <param name="report">The report being built.</param>
    /// <param name="chargeable">
    /// What may be billed today — executed and unpaid, across every month. Never includes work
    /// that has not been carried out: the tutor is not asked for money the sitter has not earned.
    /// </param>
    private async Task AddPaymentSectionAsync(ReportDocument report, decimal chargeable)
    {
        var petSitter = await repositoryPetSitter.GetAsync(session.CurrentPetSitterId).WithSync();

        // Nothing to say if there is no key on file and nothing outstanding.
        if (string.IsNullOrWhiteSpace(petSitter?.Pix) && chargeable <= 0m)
        {
            return;
        }

        var payment = new ReportSection { Heading = "Pagamento" };

        if (petSitter != null)
        {
            payment.Fields.Add(new ReportField("Favorecido", petSitter.Name));
        }

        if (!string.IsNullOrWhiteSpace(petSitter?.Pix))
        {
            payment.Fields.Add(new ReportField("Chave Pix", petSitter.Pix, true));
        }

        payment.Fields.Add(new ReportField(
            "Total a pagar",
            chargeable > 0m ? AppSession.Money(chargeable) : "Nada pendente. Obrigado!",
            chargeable > 0m));

        report.Sections.Add(payment);
    }

    private Task StartEdit()
    {
        // Seeded from the loaded record, so cancelling and reopening the editor starts from the
        // saved values again rather than from whatever was typed and abandoned.
        EditName = Name;
        EditPhone = Phone;
        EditAddress = Neighborhood;
        EditError = string.Empty;
        IsEditing = true;
        return Task.CompletedTask;
    }

    private Task CancelEdit()
    {
        EditError = string.Empty;
        IsEditing = false;
        return Task.CompletedTask;
    }

    private async Task SaveEdit()
    {
        if (session.SelectedTutorId is not int tutorId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            EditError = "Informe o nome do tutor.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditPhone))
        {
            EditError = "Informe o telefone.";
            return;
        }

        var result = await repositoryTutors.Update(new Tutors
        {
            TutorId = tutorId,
            Name = EditName.Trim(),
            Telephone = EditPhone.Trim(),
            Address = EditAddress.Trim(),
        }).WithSync();

        if (result != Response.Successful)
        {
            EditError = "Não foi possível salvar as alterações.";
            return;
        }

        IsEditing = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
    }

    private async Task Delete()
    {
        if (session.SelectedTutorId is not { } tutorId)
        {
            return;
        }

        // Cascades to this tutor's dogs and their services — see RepositoryTutors.Delete.
        await repositoryTutors.Delete(tutorId).WithSync();
        session.SelectedTutorId = null;
        session.NotifyDataChanged();
        currentView.GoBack();
    }
}