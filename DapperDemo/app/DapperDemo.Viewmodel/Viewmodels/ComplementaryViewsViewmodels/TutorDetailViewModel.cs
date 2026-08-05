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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryPayments repositoryPayments;
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
    /// Every payment this tutor has made, unfiltered. The list on screen is scoped to the chosen
    /// period, but a correction re-reads from here, and the whole history is what a reversal has to
    /// be consistent with.
    /// </summary>
    private TutorPayment[] tutorPayments = [];

    /// <summary>The payment being corrected, and the date it keeps while its amount changes.</summary>
    private int? editingPaymentId;

    private DateTime editingPaymentDate;

    /// <summary>The payment the confirm dialog is asking about.</summary>
    private int? deletingPaymentId;

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
        RepositoryPayments repositoryPayments,
        RepositoryPetSitter repositoryPetSitter,
        ReportExporter reportExporter,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.repositoryPayments = repositoryPayments;
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
        CancelPaymentEditCommand = new SynchronizedCommand(CancelPaymentEdit, SynchronizationBehavior.Discard, true);
        SavePaymentEditCommand = new SynchronizedCommand(SavePaymentEdit, SynchronizationBehavior.Discard, true);
        CancelPaymentDeleteCommand = new SynchronizedCommand(CancelPaymentDelete, SynchronizationBehavior.Discard, true);
        ConfirmPaymentDeleteCommand = new SynchronizedCommand(ConfirmPaymentDelete, SynchronizationBehavior.Discard, true);
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

    /// <summary>Gets the command that abandons a correction, leaving the payment as it was.</summary>
    public ICommand CancelPaymentEditCommand { get; }

    /// <summary>Gets the command that rewrites a payment to the amount now typed.</summary>
    public ICommand SavePaymentEditCommand { get; }

    public ICommand CancelPaymentDeleteCommand { get; }

    /// <summary>Gets the command that undoes a payment: what it settled and what it left as credit.</summary>
    public ICommand ConfirmPaymentDeleteCommand { get; }

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

    /// <summary>Gets or sets the amount received, as typed.</summary>
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

    /// <summary>
    /// Gets a value indicating whether a payment already taken is being corrected, which swaps that
    /// row for an amount box.
    /// </summary>
    public bool IsEditingPayment { get; private set; }

    /// <summary>Gets or sets the corrected amount, as typed.</summary>
    public string EditPaymentAmount { get; set; } = string.Empty;

    public string EditPaymentError { get; private set; } = string.Empty;

    public bool HasEditPaymentError => !string.IsNullOrEmpty(EditPaymentError);

    /// <summary>Gets a value indicating whether the delete-payment confirmation is up.</summary>
    public bool ConfirmingPaymentDelete { get; private set; }

    /// <summary>
    /// Gets the "replace the file already there?" question, asked mid-export on the platforms where
    /// the app names the file itself. Bound to its own dialog in the view.
    /// </summary>
    public ConfirmRequest ReplaceRequest { get; } = new();

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
    /// Gets what the tutor has handed over in the chosen period, newest first, each row able to be
    /// corrected or undone. Scoped by the same pickers as the service list above it.
    /// </summary>
    public ObservableCollection<TutorPaymentRow> Payments { get; } = [];

    /// <summary>Gets a value indicating whether nothing was received in this period, so the list can say so.</summary>
    public bool NoPayments { get; private set; }

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
        IsEditingPayment = false;
        editingPaymentId = null;
        EditPaymentError = string.Empty;
        ConfirmingPaymentDelete = false;
        deletingPaymentId = null;

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

        await ReloadServicesAsync(tutorId).WithSync();
        tutorPayments = await repositoryPayments.ListForTutorAsync(tutorId).WithSync();

        // Only executed work can be billed, which AmountDue already encodes.
        var due = tutorServices.Sum(s => s.AmountDue);
        TotalDueLabel = AppSession.Money(due);
        HasBalance = due > 0m;

        var upcoming = tutorServices.Sum(s => s.AmountUpcoming);
        UpcomingTotalLabel = AppSession.Money(upcoming);
        HasUpcoming = upcoming > 0m;

        RefreshYearOptions(tutorServices, tutorPayments.Select(p => p.Date));

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
                service.ServicePaid ? "Pago" : "Sem pagar",
                service.ServiceDone,
                service.ServiceDone ? "Feito" : "A fazer",
                openCommand));
        }

        NoPending = pending.Length == 0;

        RebuildPayments();
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        ClearPendingServices();
        ClearPayments();
        return Task.CompletedTask;
    }

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever ShowPaidServices changes.</summary>
    protected void OnShowPaidServicesChanged()
    {
        ReloadIfIdle();
        SelectedMonth = ShowPaidServices ? MonthOptions.FirstOrDefault(x => x.Number == DateTime.Now.Month) : MonthOptions[0];
    }

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

        var message = await ApplyPaymentAsync(amount, DateTime.Now, replacing: null).WithSync();
        if (message == null)
        {
            PaymentError = "Não foi possível registrar o pagamento.";
            return;
        }

        PaymentError = string.Empty;
        IsRegisteringPayment = false;
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
        PaymentMsg = message;
    }

    /// <summary>
    /// Takes an amount received and writes it: spread over what the tutor owes, remainder held as
    /// credit, both recorded as one entry in the ledger.
    /// </summary>
    /// <remarks>
    /// Correcting a payment goes through here too, undoing the old entry first and then applying
    /// the new amount to the state that leaves behind. Re-allocating from scratch rather than
    /// nudging the difference is what keeps a correction honest: raising 50 to 500 has to reach
    /// services the smaller amount never got to, and lowering it has to let go of services it
    /// should never have settled.
    /// </remarks>
    /// <param name="amount">The amount received, as corrected.</param>
    /// <param name="date">When it was received. A correction keeps the original date.</param>
    /// <param name="replacing">The payment this supersedes, or null for a new one.</param>
    /// <returns>A sentence describing what was written, or null when nothing could be written.</returns>
    private async Task<string?> ApplyPaymentAsync(decimal amount, DateTime date, int? replacing)
    {
        if (session.SelectedTutorId is not int tutorId)
        {
            return null;
        }

        if (replacing is int superseded)
        {
            if (await repositoryPayments.DeleteAsync(superseded).WithSync() != Response.Successful)
            {
                return null;
            }

            // The reversal moved settled amounts and possibly the tutor's credit, and the new
            // amount is spread over what that left rather than over the stale figures on screen.
            await ReloadServicesAsync(tutorId).WithSync();
        }

        // No allocations is not a failure: a tutor may hand money over before any of the work has
        // been carried out, and the whole of it then becomes credit.
        var (allocations, applied) = PaymentAllocation.Allocate(tutorServices, amount);
        var leftover = amount - applied;

        var written = await repositoryPayments.RecordAsync(new TutorPayment
        {
            TutorId = tutorId,
            PetSitterId = session.CurrentPetSitterId,
            Date = date,
            Amount = amount,
            CreditStored = leftover,
            Allocations = allocations,
        }).WithSync();

        if (written != Response.Successful)
        {
            return null;
        }

        var settled = allocations.Count(p => p.FullyPaid);
        var message = applied > 0m
            ? $"Recebido {AppSession.Money(applied)} — {(settled == 1 ? "1 serviço quitado" : $"{settled} serviços quitados")}."
            : string.Empty;

        if (leftover > 0m)
        {
            // More than the executed work came to, so the remainder is an advance. It is held as
            // credit in the tutor's favour and spent as each future service is booked.
            message += message.Length == 0
                ? $"Recebido {AppSession.Money(leftover)} adiantado, guardado como crédito para os próximos serviços."
                : $" {AppSession.Money(leftover)} ficam como crédito para os próximos serviços.";
        }

        return message;
    }

    /// <summary>Opens one payment for correction, seeded with what was received.</summary>
    private Task EditPayment(TutorPayment payment)
    {
        editingPaymentId = payment.TutorPaymentId;
        editingPaymentDate = payment.Date;

        // Comma, like the rest of the app's money: TryParseAmount accepts either separator, and the
        // box should open showing the amount the way the sitter would have typed it.
        EditPaymentAmount = payment.Amount.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
        EditPaymentError = string.Empty;
        PaymentMsg = string.Empty;
        IsRegisteringPayment = false;
        IsEditingPayment = true;
        return Task.CompletedTask;
    }

    private Task CancelPaymentEdit()
    {
        editingPaymentId = null;
        EditPaymentError = string.Empty;
        IsEditingPayment = false;
        return Task.CompletedTask;
    }

    private async Task SavePaymentEdit()
    {
        if (editingPaymentId is not int paymentId)
        {
            return;
        }

        if (!TryParseAmount(EditPaymentAmount, out var amount) || amount <= 0m)
        {
            EditPaymentError = "Informe um valor válido.";
            return;
        }

        var message = await ApplyPaymentAsync(amount, editingPaymentDate, paymentId).WithSync();

        // Either half of the correction may have written before it failed, so the screen is reloaded
        // whatever happened rather than left showing figures that are no longer true.
        session.NotifyDataChanged();
        await ReloadAsync().WithSync();

        PaymentMsg = message == null
            ? "Não foi possível corrigir o pagamento. Confira o pagamento na lista antes de tentar de novo."
            : $"Pagamento corrigido para {AppSession.Money(amount)}. {message}";
    }

    private Task AskDeletePayment(TutorPayment payment)
    {
        deletingPaymentId = payment.TutorPaymentId;
        PaymentMsg = string.Empty;
        ConfirmingPaymentDelete = true;
        return Task.CompletedTask;
    }

    private Task CancelPaymentDelete()
    {
        deletingPaymentId = null;
        ConfirmingPaymentDelete = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Undoes a payment entirely: the services it settled go back to owing, and what it left as
    /// credit is taken off the tutor — followed into the bookings it was since spent on if the
    /// balance no longer holds it.
    /// </summary>
    private async Task ConfirmPaymentDelete()
    {
        if (deletingPaymentId is not int paymentId)
        {
            return;
        }

        var payment = tutorPayments.FirstOrDefault(p => p.TutorPaymentId == paymentId);
        var undone = await repositoryPayments.DeleteAsync(paymentId).WithSync();

        ConfirmingPaymentDelete = false;
        deletingPaymentId = null;

        if (undone != Response.Successful)
        {
            // Not PaymentError: that one lives inside the amount form, which is not open here.
            PaymentMsg = "Não foi possível excluir o pagamento.";
            return;
        }

        session.NotifyDataChanged();
        await ReloadAsync().WithSync();
        PaymentMsg = payment == null
            ? "Pagamento excluído."
            : $"Pagamento de {AppSession.Money(payment.Amount)} excluído.";
    }

    /// <summary>
    /// Re-reads this tutor's services. Used both by the full reload and between the two halves of
    /// a correction, where the second half must see what the first left behind.
    /// </summary>
    private async Task ReloadServicesAsync(int tutorId)
    {
        var services = await repositoryServices.ListForTutorAsync(session.CurrentPetSitterId, tutorId).WithSync();
        tutorServices = [.. services.OrderBy(s => s.Date).ThenBy(s => s.ServiceId)];
    }

    /// <summary>Fills the payment list with what the tutor handed over inside the chosen period.</summary>
    private void RebuildPayments()
    {
        ClearPayments();

        var scoped = tutorPayments
            .Where(p => ServicePeriod.Matches(p.Date, SelectedMonth, SelectedYear))
            .ToArray();

        foreach (var payment in scoped)
        {
            // CA2000: ownership passes to the row, which disposes both commands when the list is
            // rebuilt — see ClearPayments.
#pragma warning disable CA2000
            var editCommand = new SynchronizedCommand(() => EditPayment(payment), SynchronizationBehavior.Discard, true);
            var deleteCommand = new SynchronizedCommand(() => AskDeletePayment(payment), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000

            Payments.Add(new TutorPaymentRow(
                payment.TutorPaymentId,
                AppSession.DateTimeLabel(payment.Date),
                AppSession.Money(payment.Amount),
                payment.CreditStored > 0m ? $"{AppSession.Money(payment.CreditStored)} em crédito" : string.Empty,
                editCommand,
                deleteCommand));
        }

        NoPayments = scoped.Length == 0;
    }

    private void ClearPayments()
    {
        foreach (var row in Payments)
        {
            row.Dispose();
        }

        Payments.Clear();
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
            // Outstanding, not AmountDue: a payment now settles work that has not been carried out,
            // and AmountDue is zero for exactly those — which would report a negative remainder.
            var service = tutorServices.First(s => s.Kind == partial.Kind && s.ServiceId == partial.ServiceId);
            var remainder = service.Outstanding - partial.Amount;

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
    private void RefreshYearOptions(ServiceItem[] services, IEnumerable<DateTime> paymentDates)
    {
        var years = ServicePeriod.Years(services, paymentDates);
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
                    service.ServicePaid ? "Pago" : "Sem pagar"));
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
        var fileName = await reportExporter
            .ExportAsync(report, $"servicos-{slug}", AskReplaceAsync)
            .WithSync();

        ExportMsg = fileName == null ? string.Empty : $"Resumo salvo: {fileName}";
    }

    /// <summary>
    /// Answers the export's "there is already one of these" question through the screen's dialog.
    /// </summary>
    private Task<bool> AskReplaceAsync(string fileName) =>
        ReplaceRequest.AskAsync($"Já existe um arquivo chamado {fileName} nesta pasta. Substituir?");

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