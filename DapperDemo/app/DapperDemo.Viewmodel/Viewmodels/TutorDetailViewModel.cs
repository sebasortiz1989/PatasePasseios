using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly CurrentView currentView;

    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;

    /// <summary>
    /// Every service for this tutor's dogs, past and future. Held because a payment is settled
    /// against the whole history, not just what is still to come.
    /// </summary>
    private ServiceItem[] tutorServices = [];

    public TutorDetailViewModel(
        CurrentView currentView,
        RepositoryTutors repositoryTutors,
        RepositoryDogs repositoryDogs,
        RepositoryServices repositoryServices,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryTutors = repositoryTutors;
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
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

    /// <summary>Gets everything this tutor still owes, across every month.</summary>
    public string TotalDueLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether there is anything to collect, so the button can hide when there is not.</summary>
    public bool HasBalance { get; private set; }

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

    public bool NoFuture { get; private set; }

    public ObservableCollection<TutorFutureServiceRow> FutureServices { get; } = [];

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
        Neighborhood = tutor.Address ?? string.Empty;
        Phone = tutor.Telephone;

        var dogs = await repositoryDogs.ListForTutorAsync(tutorId).WithSync();
        DogNames = dogs.Length == 0 ? "Nenhum cachorro cadastrado." : string.Join(", ", dogs.Select(d => d.Name));

        var services = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var dogIds = dogs.Select(d => d.DogId).ToHashSet();
        tutorServices = [.. services.Where(s => dogIds.Contains(s.DogId)).OrderBy(s => s.Date).ThenBy(s => s.ServiceId)];

        var due = tutorServices.Sum(s => s.AmountDue);
        TotalDueLabel = AppSession.Money(due);
        HasBalance = due > 0m;

        var now = DateTime.Now;
        var future = tutorServices
            .Where(s => s.Date >= now)
            .ToArray();

        ClearFutureServices();
        foreach (var service in future)
        {
            // CA2000: ownership passes to the row, which disposes the command when the list is
            // rebuilt — see ClearFutureServices.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(
                () => Open(service.Kind, service.ServiceId),
                SynchronizationBehavior.Discard,
                true);
#pragma warning restore CA2000

            FutureServices.Add(new TutorFutureServiceRow(
                service.DogName,
                AppSession.TypeLabel(service.Kind),
                AppSession.DateTimeLabel(service.Date, service.Kind),
                service.ServicePaid,
                service.ServicePaid ? "Pago" : "Pendente",
                openCommand));
        }

        NoFuture = future.Length == 0;
    }

    /// <summary>
    /// Spreads an amount received over what a tutor owes, oldest service first.
    /// </summary>
    /// <remarks>
    /// Static and free of state so the rule can be reasoned about on its own. Services the money
    /// covers in full keep their price and are marked paid. The one service the money runs out on
    /// has its price cut to the remainder and stays unpaid — so a 100 service part-paid by 75
    /// becomes an unpaid 25. Anything left once every service is settled has nowhere to go: the
    /// caller reports it rather than storing a credit.
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="amount">The amount received.</param>
    /// <returns>The services to write, and how much of the payment was actually used.</returns>
    internal static (List<ServicePayment> Payments, decimal Applied) AllocatePayment(IEnumerable<ServiceItem> services, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(services);

        var payments = new List<ServicePayment>();
        var remaining = amount;

        // Oldest first, so the debt that has been outstanding longest is cleared first. ServiceId
        // breaks ties, which keeps the order stable for two services booked at the same moment.
        foreach (var service in services.Where(s => !s.ServicePaid).OrderBy(s => s.Date).ThenBy(s => s.ServiceId))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var due = service.AmountDue;
            if (remaining >= due)
            {
                remaining -= due;
                payments.Add(new ServicePayment(service.Kind, service.ServiceId, service.Price, true));
                continue;
            }

            // Falls short: the price becomes what is still owed. A hotel stay prices per night,
            // so the remainder is divided back out over the nights it spans.
            var shortfall = due - remaining;
            var newPrice = service.Kind == ServiceKind.Hotel
                ? decimal.Round(shortfall / service.Nights, 2, MidpointRounding.AwayFromZero)
                : shortfall;

            payments.Add(new ServicePayment(service.Kind, service.ServiceId, newPrice, false));
            remaining = 0m;
            break;
        }

        return (payments, amount - remaining);
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        ClearFutureServices();
        return Task.CompletedTask;
    }

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

        var (payments, applied) = AllocatePayment(tutorServices, amount);
        if (payments.Count == 0)
        {
            PaymentError = "Este tutor não tem serviços pendentes.";
            return;
        }

        var result = await repositoryServices.RegisterPaymentAsync(payments).WithSync();
        if (result != Response.Successful)
        {
            PaymentError = "Não foi possível registrar o pagamento.";
            return;
        }

        var settled = payments.Count(p => p.FullyPaid);
        var leftover = amount - applied;
        var message = $"Recebido {AppSession.Money(applied)} — {(settled == 1 ? "1 serviço quitado" : $"{settled} serviços quitados")}.";

        if (leftover > 0m)
        {
            // Nowhere to put a credit, so it is reported rather than silently swallowed.
            message += $" Sobraram {AppSession.Money(leftover)} além do que era devido.";
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

        var (payments, applied) = AllocatePayment(tutorServices, amount);
        if (payments.Count == 0)
        {
            return "Nada pendente para este tutor.";
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
                ? $". Sobram {AppSession.Money(leftover)} além do devido."
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

    private void ClearFutureServices()
    {
        foreach (var row in FutureServices)
        {
            row.Dispose();
        }

        FutureServices.Clear();
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