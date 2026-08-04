using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Globalization;
using System.Text;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class ServiceDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryServices repositoryServices;
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly CreditSpender creditSpender;
    private readonly AppSession session;
    private readonly UriLauncher uriLauncher;

    /// <summary>The record as last read, so an edit can be saved without re-reading it.</summary>
    private ServiceItem? current;

    public ServiceDetailViewModel(
        CurrentView currentView,
        RepositoryServices repositoryServices,
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        CreditSpender creditSpender,
        AppSession session,
        UriLauncher uriLauncher)
    {
        this.repositoryServices = repositoryServices;
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.creditSpender = creditSpender;
        this.session = session;
        this.uriLauncher = uriLauncher;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        AddToCalendarCommand = new SynchronizedCommand(AddToCalendar, SynchronizationBehavior.Discard, true);
        ToggleDoneCommand = new SynchronizedCommand(ToggleDone, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand AddToCalendarCommand { get; }

    public ICommand ToggleDoneCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the screen is in edit mode — see
    /// <see cref="DogDetailViewModel.IsEditing"/> for why the editor replaces the fields in place.
    /// </summary>
    public bool IsEditing { get; private set; }

    public bool IsViewing => !IsEditing;

    public string TypeLabel { get; private set; } = string.Empty;

    public string DogName { get; private set; } = string.Empty;

    /// <summary>Gets the dog's initials, shown when it has no photo.</summary>
    public string DogInitials { get; private set; } = string.Empty;

    /// <summary>Gets the booked dog's photo, or null when it has none.</summary>
    public string? DogImagePath { get; private set; }

    public bool HasDogImage => DogImagePath != null;

    public bool NoDogImage => !HasDogImage;

    public string TutorName { get; private set; } = string.Empty;

    public string DateLabel { get; private set; } = string.Empty;

    public bool IsHotel { get; private set; }

    /// <summary>Gets a value indicating whether this is a day-care booking: one day, no check-out, no time.</summary>
    public bool IsDayCare { get; private set; }

    /// <summary>Gets a value indicating whether the walks row applies — hotel stays and day-care carry one.</summary>
    public bool ShowsWalking => IsHotel || IsDayCare;

    public string EndLabel { get; private set; } = string.Empty;

    public string WalkingLabel { get; private set; } = string.Empty;

    public string PriceFieldLabel { get; private set; } = string.Empty;

    public string PriceLabel { get; private set; } = string.Empty;

    /// <summary>Gets how many nights the stay covers, e.g. "3 diárias". Hotel stays only.</summary>
    public string DaysLabel { get; private set; } = string.Empty;

    /// <summary>Gets the daily rate times the nights, plus any extra. Hotel stays only.</summary>
    public string TotalLabel { get; private set; } = string.Empty;

    /// <summary>Gets how much of this service has been settled, however it was funded.</summary>
    public string SettledLabel { get; private set; } = string.Empty;

    /// <summary>Gets what is still unsettled on this service.</summary>
    public string OutstandingLabel { get; private set; } = string.Empty;

    /// <summary>Gets the sentence naming the part covered by the tutor's credit, or empty.</summary>
    public string CreditNote { get; private set; } = string.Empty;

    public bool HasCreditNote => !string.IsNullOrEmpty(CreditNote);

    /// <summary>
    /// Gets a value indicating whether the settled/outstanding split is worth showing. Only when
    /// something has been settled but the service is not yet fully paid — otherwise the single
    /// price line already says everything.
    /// </summary>
    public bool HasPartialSettlement { get; private set; }

    /// <summary>Gets the one-off extra on a hotel stay, formatted.</summary>
    public string ExtraChargeLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this stay carries an extra worth showing.</summary>
    public bool HasExtraCharge { get; private set; }

    public bool Paid { get; private set; }

    public string PaidActionLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the work has been carried out — see <see cref="ServiceItem.ServiceDone"/>.</summary>
    public bool Done { get; private set; }

    public string DoneActionLabel { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether to explain why the payment chip is closed off.</summary>
    public bool ShowsPaymentBlockedHint => !Done && !Paid;

    /// <summary>Gets what happened to the tutor's credit when this booking was marked done, or empty.</summary>
    public string CreditMsg { get; private set; } = string.Empty;

    public bool HasCreditMsg => !string.IsNullOrEmpty(CreditMsg);

    // Avalonia has no datetime-local control, so date and time are edited separately, the same
    // way the new-service form splits them.
    public DateTime EditDatePart { get; set; }

    public TimeSpan EditTimePart { get; set; }

    public DateTime EditEndDatePart { get; set; }

    public TimeSpan EditEndTimePart { get; set; }

    public string EditPrice { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a one-off amount charged on top of a hotel stay — a late pick-up, say. Blank means none.
    /// </summary>
    public string EditExtraCharge { get; set; } = string.Empty;

    public bool EditRequiresWalking { get; set; }

    /// <summary>Gets the label above the price input: a hotel's figure is a daily rate.</summary>
    public string EditPriceLabel => IsHotel ? "Preço por dia (R$)" : "Preço (R$)";

    public string EditError { get; private set; } = string.Empty;

    public bool HasEditError => !string.IsNullOrEmpty(EditError);

    /// <summary>
    /// Public because the View calls it from OnLoaded — see <see cref="DogDetailViewModel"/> for
    /// why OnRunStarting is not enough for a screen shown through CurrentView.
    /// </summary>
    public Task ReloadAsync() => LoadAsync();

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    /// <summary>
    /// Nights billed for a stay. Check-in and check-out on the same day still bills one, which is
    /// how a day rate is normally charged and keeps the total from coming out as zero.
    /// </summary>
    private static int NightsBetween(DateTime start, DateTime? end) =>
        end is DateTime finish ? Math.Max((finish.Date - start.Date).Days, 1) : 1;

    /// <summary>
    /// Google Calendar's pre-filled event link. It opens the compose screen with everything
    /// entered — the user still presses Save, so the app never needs calendar access of its own.
    /// </summary>
    /// <remarks>
    /// Times are sent without a zone designator on purpose. Google reads a naive value in the
    /// calendar's own timezone, which is what the sitter booked in; sending UTC would mean
    /// converting from this device's clock and being an hour out whenever the two disagree.
    /// </remarks>
    private static Uri BuildCalendarUri(ServiceItem service)
    {
        // All-day events are a half-open range, so a one-day booking ends the following day.
        var dates = service.Kind == ServiceKind.DayCare
            ? $"{service.Date:yyyyMMdd}/{service.Date.AddDays(1):yyyyMMdd}"
            : $"{service.Date:yyyyMMddTHHmmss}/{EndOf(service):yyyyMMddTHHmmss}";

        var details = new StringBuilder();
        details.Append("Tutor: ").Append(service.TutorName);
        details.Append("\nCachorro: ").Append(service.DogName);
        details.Append("\nServiço: ").Append(AppSession.TypeLabel(service.Kind));

        if (service.Kind == ServiceKind.Hotel)
        {
            var nights = NightsBetween(service.Date, service.EndDate);
            details.Append("\nDiárias: ").Append(nights.ToString(CultureInfo.InvariantCulture));
            details.Append("\nValor: ").Append(AppSession.Money(service.Price * nights));
        }
        else
        {
            details.Append("\nValor: ").Append(AppSession.Money(service.Price));
        }

        if (service.Kind is ServiceKind.Hotel or ServiceKind.DayCare)
        {
            details.Append("\nPasseios: ").Append(service.RequiresWalking ? "Incluídos" : "Não incluídos");
        }

        details.Append("\nPagamento: ").Append(service.ServicePaid ? "Pago" : "Sem pagar");

        var url = new StringBuilder("https://calendar.google.com/calendar/render?action=TEMPLATE");
        url.Append("&text=").Append(Uri.EscapeDataString($"{AppSession.TypeLabel(service.Kind)} — {service.DogName}"));
        url.Append("&dates=").Append(dates);
        url.Append("&details=").Append(Uri.EscapeDataString(details.ToString()));

        // Omitted rather than sent empty when the tutor has no address on file: Google shows the
        // location row whenever the parameter is present, so a blank one just looks broken.
        if (!string.IsNullOrWhiteSpace(service.TutorAddress))
        {
            url.Append("&location=").Append(Uri.EscapeDataString(service.TutorAddress.Trim()));
        }

        return new Uri(url.ToString());
    }

    /// <summary>
    /// When a booking finishes. A stay runs to its check-out; a walk or sitting has no stored
    /// duration, so it gets an hour — enough for the event to occupy a readable slot.
    /// </summary>
    private static DateTime EndOf(ServiceItem service) => service.Kind == ServiceKind.Hotel
        ? service.EndDate ?? service.Date.AddDays(1)
        : service.Date.AddHours(1);

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price);

    private async Task AddToCalendar()
    {
        if (current is not ServiceItem service)
        {
            return;
        }

        await uriLauncher.LaunchAsync(BuildCalendarUri(service)).WithSync();
    }

    /// <summary>
    /// Marks the booking as carried out, or takes the mark back off. This is the screen where the
    /// flag is set, the agenda's tag being the shortcut.
    /// </summary>
    /// <remarks>
    /// Carrying the work out is what makes it billable, so this is also the moment any credit the
    /// tutor is holding gets spent — see <see cref="CreditSpender"/>. Un-marking spends nothing;
    /// money that has already moved is not taken back by correcting a tick.
    /// </remarks>
    private async Task ToggleDone()
    {
        if (session.SelectedServiceKind is not ServiceKind kind || session.SelectedServiceId is not int serviceId)
        {
            return;
        }

        var nowDone = !Done;
        await repositoryServices.SetDoneAsync(kind, serviceId, nowDone).WithSync();

        var spent = nowDone && current is ServiceItem service
            ? await creditSpender.SpendForDogAsync(session.CurrentPetSitterId, service.DogId).WithSync()
            : string.Empty;

        session.NotifyDataChanged();

        // After the reload, which clears the message so it belongs to this action rather than
        // lingering from a previous one.
        await LoadAsync().WithSync();
        CreditMsg = spent.Trim();
    }

    private Task StartEdit()
    {
        if (current is not ServiceItem service)
        {
            return Task.CompletedTask;
        }

        EditDatePart = service.Date.Date;
        EditTimePart = service.Date.TimeOfDay;
        EditEndDatePart = (service.EndDate ?? service.Date.AddDays(1)).Date;
        EditEndTimePart = (service.EndDate ?? service.Date.AddDays(1)).TimeOfDay;
        EditPrice = service.Price.ToString("0.##", CultureInfo.InvariantCulture);
        EditExtraCharge = service.ExtraCharge > 0m
            ? service.ExtraCharge.ToString("0.##", CultureInfo.InvariantCulture)
            : string.Empty;
        EditRequiresWalking = service.RequiresWalking;
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
        if (current is not ServiceItem service)
        {
            return;
        }

        if (!TryParsePrice(EditPrice, out var price) || price <= 0)
        {
            EditError = "Informe um preço válido.";
            return;
        }

        // Blank means none; anything else has to be a real amount rather than being ignored.
        var extraCharge = 0m;
        if (!string.IsNullOrWhiteSpace(EditExtraCharge) && (!TryParsePrice(EditExtraCharge, out extraCharge) || extraCharge < 0m))
        {
            EditError = "Informe um valor adicional válido.";
            return;
        }

        // Day-care is booked for a day, not a moment, so the time picker is not shown for it and
        // its date is stored bare.
        var date = service.Kind == ServiceKind.DayCare
            ? EditDatePart.Date
            : EditDatePart.Date + EditTimePart;
        var endDate = EditEndDatePart.Date + EditEndTimePart;

        if (service.Kind == ServiceKind.Hotel && endDate <= date)
        {
            EditError = "A saída deve ser depois da entrada.";
            return;
        }

        var result = await repositoryServices.UpdateAsync(new ServiceItem
        {
            ServiceId = service.ServiceId,
            Kind = service.Kind,
            DogId = service.DogId,
            DogName = service.DogName,
            TutorName = service.TutorName,
            Date = date,
            EndDate = service.Kind == ServiceKind.Hotel ? endDate : null,
            Price = price,
            ExtraCharge = service.Kind == ServiceKind.Hotel ? extraCharge : 0m,
            RequiresWalking = EditRequiresWalking,
            ServicePaid = service.ServicePaid,
            ServiceDone = service.ServiceDone,
        }).WithSync();

        if (result != Response.Successful)
        {
            EditError = "Não foi possível salvar as alterações.";
            return;
        }

        IsEditing = false;
        session.NotifyDataChanged();
        await LoadAsync().WithSync();
    }

    private async Task Delete()
    {
        if (session.SelectedServiceKind is not ServiceKind kind || session.SelectedServiceId is not int serviceId)
        {
            return;
        }

        // Credit put against this booking goes back to the tutor before the row disappears.
        // Without this the money the tutor handed over would simply vanish with the service.
        await RefundCreditAsync(current).WithSync();

        await repositoryServices.DeleteAsync(kind, serviceId).WithSync();
        session.SelectedServiceKind = null;
        session.SelectedServiceId = null;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }

    /// <summary>Returns a deleted service's applied credit to the tutor who funded it.</summary>
    private async Task RefundCreditAsync(ServiceItem? service)
    {
        if (service is not { CreditApplied: > 0m })
        {
            return;
        }

        var dog = await repositoryDogs.GetAsync(service.DogId).WithSync();
        if (dog == null)
        {
            return;
        }

        var tutor = await repositoryTutors.GetAsync(dog.TutorId).WithSync();
        if (tutor == null)
        {
            return;
        }

        await repositoryTutors.SetCreditAsync(dog.TutorId, tutor.Credit + service.CreditApplied).WithSync();
    }

    private async Task LoadAsync()
    {
        // Before the early returns below, so arriving here always lands on the reading state —
        // leaving mid-edit and coming back should not resume a half-finished form.
        IsEditing = false;
        EditError = string.Empty;
        ConfirmingDelete = false;
        CreditMsg = string.Empty;

        if (session.SelectedServiceKind is not ServiceKind kind || session.SelectedServiceId is not int serviceId)
        {
            return;
        }

        var service = await repositoryServices.GetAsync(session.CurrentPetSitterId, kind, serviceId).WithSync();
        if (service == null)
        {
            return;
        }

        current = service;

        TypeLabel = AppSession.TypeLabel(service.Kind);
        DogName = service.DogName;
        DogInitials = AppSession.Initials(service.DogName);
        DogImagePath = DogImageStore.ResolvePath(service.DogImage);
        TutorName = service.TutorName;
        DateLabel = AppSession.DateTimeLabel(service.Date, service.Kind);
        IsHotel = service.Kind == ServiceKind.Hotel;
        IsDayCare = service.Kind == ServiceKind.DayCare;
        EndLabel = service.EndDate is DateTime end ? AppSession.DateTimeLabel(end) : string.Empty;
        WalkingLabel = service.RequiresWalking ? "Incluídos" : "Não incluídos";
        PriceFieldLabel = service.Kind == ServiceKind.Hotel ? "Preço por dia" : "Preço";
        PriceLabel = service.Kind == ServiceKind.Hotel
            ? AppSession.Money(service.Price) + " / dia"
            : AppSession.Money(service.Price);

        // A stay is entered as a daily rate, so what it actually costs is only visible if the
        // screen does the multiplication.
        ExtraChargeLabel = AppSession.Money(service.ExtraCharge);
        HasExtraCharge = service.Kind == ServiceKind.Hotel && service.ExtraCharge > 0m;

        var nights = NightsBetween(service.Date, service.EndDate);
        DaysLabel = nights == 1 ? "1 diária" : $"{nights} diárias";
        TotalLabel = AppSession.Money(service.Total);

        SettledLabel = AppSession.Money(service.AmountSettled);
        OutstandingLabel = AppSession.Money(service.Outstanding);
        HasPartialSettlement = !service.ServicePaid && service.AmountSettled > 0m;

        // Names where the money came from, which is the whole point of tracking credit separately
        // from a payment — the sitter needs to recognise a service they were never handed cash for.
        CreditNote = service.CreditApplied > 0m
            ? $"{AppSession.Money(service.CreditApplied)} deste serviço foram pagos com o crédito do tutor."
            : string.Empty;

        Paid = service.ServicePaid;
        PaidActionLabel = service.ServicePaid ? "Pago" : "Sem pagar";

        Done = service.ServiceDone;
        DoneActionLabel = service.ServiceDone ? "Feito" : "A fazer";
    }
}