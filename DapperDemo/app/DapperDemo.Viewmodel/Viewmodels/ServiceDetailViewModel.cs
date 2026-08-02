using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Mensagens.Dapper.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class ServiceDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;

    /// <summary>The record as last read, so an edit can be saved without re-reading it.</summary>
    private ServiceItem? current;

    public ServiceDetailViewModel(
        CurrentView currentView,
        RepositoryServices repositoryServices,
        AppSession session)
    {
        this.repositoryServices = repositoryServices;
        this.session = session;
        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        TogglePaidCommand = new SynchronizedCommand(TogglePaid, SynchronizationBehavior.Discard, true);
        AskDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = true, SynchronizationBehavior.Discard, true);
        CancelDeleteCommand = new SynchronizedCommand(() => ConfirmingDelete = false, SynchronizationBehavior.Discard, true);
        ConfirmDeleteCommand = new SynchronizedCommand(Delete, SynchronizationBehavior.Discard, true);
        EditCommand = new SynchronizedCommand(StartEdit, SynchronizationBehavior.Discard, true);
        CancelEditCommand = new SynchronizedCommand(CancelEdit, SynchronizationBehavior.Discard, true);
        SaveEditCommand = new SynchronizedCommand(SaveEdit, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand TogglePaidCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SaveEditCommand { get; }

    /// <summary>Gets a value indicating whether deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    public bool NotConfirmingDelete => !ConfirmingDelete;

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

    public string EndLabel { get; private set; } = string.Empty;

    public string WalkingLabel { get; private set; } = string.Empty;

    public string PriceFieldLabel { get; private set; } = string.Empty;

    public string PriceLabel { get; private set; } = string.Empty;

    /// <summary>Gets how many nights the stay covers, e.g. "3 diárias". Hotel stays only.</summary>
    public string DaysLabel { get; private set; } = string.Empty;

    /// <summary>Gets the daily rate multiplied by <see cref="DaysLabel"/>'s count. Hotel stays only.</summary>
    public string TotalLabel { get; private set; } = string.Empty;

    public bool Paid { get; private set; }

    public string PaidActionLabel { get; private set; } = string.Empty;

    // Avalonia has no datetime-local control, so date and time are edited separately, the same
    // way the new-service form splits them.
    public DateTime EditDatePart { get; set; }

    public TimeSpan EditTimePart { get; set; }

    public DateTime EditEndDatePart { get; set; }

    public TimeSpan EditEndTimePart { get; set; }

    public string EditPrice { get; set; } = string.Empty;

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

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price);

    private async Task TogglePaid()
    {
        if (session.SelectedServiceKind is not ServiceKind kind || session.SelectedServiceId is not int serviceId)
        {
            return;
        }

        await repositoryServices.SetPaidAsync(kind, serviceId, !Paid).WithSync();
        session.NotifyDataChanged();
        await LoadAsync().WithSync();
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

        var date = EditDatePart.Date + EditTimePart;
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
            RequiresWalking = EditRequiresWalking,
            ServicePaid = service.ServicePaid,
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

        await repositoryServices.DeleteAsync(kind, serviceId).WithSync();
        session.SelectedServiceKind = null;
        session.SelectedServiceId = null;
        session.NotifyDataChanged();
        BackCommand.Execute(null);
    }

    private async Task LoadAsync()
    {
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
        IsEditing = false;
        ConfirmingDelete = false;

        TypeLabel = AppSession.TypeLabel(service.Kind);
        DogName = service.DogName;
        DogInitials = AppSession.Initials(service.DogName);
        DogImagePath = DogImageStore.ResolvePath(service.DogImage);
        TutorName = service.TutorName;
        DateLabel = AppSession.DateTimeLabel(service.Date);
        IsHotel = service.Kind == ServiceKind.Hotel;
        EndLabel = service.EndDate is DateTime end ? AppSession.DateTimeLabel(end) : string.Empty;
        WalkingLabel = service.RequiresWalking ? "Incluídos" : "Não incluídos";
        PriceFieldLabel = service.Kind == ServiceKind.Hotel ? "Preço por dia" : "Preço";
        PriceLabel = service.Kind == ServiceKind.Hotel
            ? AppSession.Money(service.Price) + " / dia"
            : AppSession.Money(service.Price);

        // A stay is entered as a daily rate, so what it actually costs is only visible if the
        // screen does the multiplication.
        var nights = NightsBetween(service.Date, service.EndDate);
        DaysLabel = nights == 1 ? "1 diária" : $"{nights} diárias";
        TotalLabel = AppSession.Money(service.Price * nights);

        Paid = service.ServicePaid;
        PaidActionLabel = service.ServicePaid ? "Pago — marcar pendente" : "Marcar como pago";
    }
}