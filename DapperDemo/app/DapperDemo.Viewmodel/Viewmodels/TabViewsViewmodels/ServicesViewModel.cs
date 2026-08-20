using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;

[AddINotifyPropertyChangedInterface]
public class ServicesViewModel : PresentationModelBase<Unit, Unit>
{
    /// <summary>The time a booking's first visit opens at, and what an empty form starts on.</summary>
    private static readonly TimeSpan FirstTimeOfDay = TimeSpan.FromHours(9);

    /// <summary>The latest a visit may be scheduled. Later would spill into the following day.</summary>
    private static readonly TimeSpan LastTimeOfDay = TimeSpan.FromHours(23);

    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryTutors repositoryTutors;
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly CreditSpender creditSpender;
    private readonly EventHandler dataChangedHandler;

    public ServicesViewModel(
        RepositoryDogs repositoryDogs,
        RepositoryTutors repositoryTutors,
        RepositoryServices repositoryServices,
        AppSession session,
        CreditSpender creditSpender)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryTutors = repositoryTutors;
        this.repositoryServices = repositoryServices;
        this.session = session;
        this.creditSpender = creditSpender;

        // A dog added on the Cachorros tab has to show up in this picker without a relaunch.
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadDogsAsync());
        session.DataChanged += dataChangedHandler;

        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);
        SetTypeDayCare = new SynchronizedCommand(() => SetType(ServiceKind.DayCare), SynchronizationBehavior.Discard, true);
        CreateServiceCommand = new SynchronizedCommand(CreateService, SynchronizationBehavior.Discard, true);

        AddTimeCommand = new SynchronizedCommand(AddTime, SynchronizationBehavior.Discard, true);
        AddDateCommand = new SynchronizedCommand(AddDate, SynchronizationBehavior.Discard, true);

        SvcDatePart = DateTime.Now.Date.AddDays(1);
        SvcTimePart = FirstTimeOfDay;
        SvcRepeatUntilPart = DateTime.Now.Date.AddDays(7);
        AddTimeSlot(FirstTimeOfDay);
        SvcRangeFromPart = DateTime.Now.Date.AddDays(1);
        AddDateSlot(DateTime.Now.Date.AddDays(1));
        SvcEndDatePart = DateTime.Now.Date.AddDays(3);
        SvcEndTimePart = TimeSpan.FromHours(18);
    }

    public ICommand SetTypeWalk { get; }

    public ICommand SetTypeSitting { get; }

    public ICommand SetTypeHotel { get; }

    public ICommand SetTypeDayCare { get; }

    public ICommand CreateServiceCommand { get; }

    public ICommand AddTimeCommand { get; }

    public ICommand AddDateCommand { get; }

    public ObservableCollection<DogOption> DogOptions { get; } = [];

    /// <summary>Gets a value indicating whether no dogs yet means the form can't do anything useful, so it explains what to do first.</summary>
    public bool HasNoDogs { get; private set; } = true;

    public bool HasDogs => !HasNoDogs;

    public ServiceKind SvcType { get; set; } = ServiceKind.Walk;

    public bool IsTypeWalk => SvcType == ServiceKind.Walk;

    public bool IsTypeSitting => SvcType == ServiceKind.Sitting;

    public bool IsTypeHotel => SvcType == ServiceKind.Hotel;

    public bool IsTypeDayCare => SvcType == ServiceKind.DayCare;

    public bool SvcIsHotel => SvcType == ServiceKind.Hotel;

    /// <summary>Gets a value indicating whether the day-picking section is shown — everything except a hotel stay.</summary>
    public bool SvcIsSingleDate => SvcType != ServiceKind.Hotel;

    /// <summary>
    /// Gets a value indicating whether the times-of-day section applies. Day-care is booked for a
    /// day, not a time, so it picks days like a walk but has no clock.
    /// </summary>
    public bool SvcUsesTimes => SvcType is ServiceKind.Walk or ServiceKind.Sitting;

    public bool SvcIsDayCare => SvcType == ServiceKind.DayCare;

    public DogOption? SelectedDog { get; set; }

    /// <summary>
    /// Gets the tutor of the dog currently picked. A service is stored against the dog, but the tutor
    /// is who the booking is with — showing it confirms the right one was chosen before saving.
    /// Fody raises this whenever <see cref="SelectedDog"/> changes, since it reads that property.
    /// </summary>
    public string SelectedTutorName => SelectedDog?.TutorName ?? string.Empty;

    /// <summary>Gets a value indicating whether whether there is a tutor to show, so the form can hide the row before a dog is picked.</summary>
    public bool HasSelectedTutor => !string.IsNullOrEmpty(SelectedTutorName);

    // Avalonia has no datetime-local control, so date and time are picked separately and recombined.
    public DateTime SvcDatePart { get; set; }

    public TimeSpan SvcTimePart { get; set; }

    public DateTime SvcEndDatePart { get; set; }

    public TimeSpan SvcEndTimePart { get; set; }

    public DateTime SvcDate => SvcDatePart.Date + SvcTimePart;

    public DateTime SvcEndDate => SvcEndDatePart.Date + SvcEndTimePart;

    /// <summary>
    /// Gets or sets a value indicating whether switches the date section between a list of individual days and a continuous range.
    /// Scattered days (the 1st, 3rd and 10th) are the common case, so the list is the default.
    /// Hotel bookings already span a range and use neither.
    /// </summary>
    public bool SvcUseDateRange { get; set; }

    public bool SvcUsesDateList => !SvcUseDateRange;

    /// <summary>
    /// Gets the individual days a service happens, when not using a range. Always holds at least one.
    /// </summary>
    public ObservableCollection<ServiceDateSlot> SvcDates { get; } = [];

    /// <summary>Gets or sets first day of a range, used only when <see cref="SvcUseDateRange"/> is set.</summary>
    public DateTime SvcRangeFromPart { get; set; }

    /// <summary>Gets or sets last day of a range, inclusive.</summary>
    public DateTime SvcRepeatUntilPart { get; set; }

    /// <summary>
    /// Gets the times of day the service happens — one row per occurrence, so a sitting can run
    /// morning and night. Always holds at least one entry.
    /// </summary>
    public ObservableCollection<ServiceTimeSlot> SvcTimes { get; } = [];

    /// <summary>Gets a value indicating whether whether the recurrence controls apply at all (everything except a hotel stay).</summary>
    public bool SvcSupportsRecurrence => SvcType != ServiceKind.Hotel;

    /// <summary>Gets how many services the current settings would create.</summary>
    public int SvcOccurrenceCount => SvcSupportsRecurrence
        ? DayCount * (SvcUsesTimes ? Math.Max(SvcTimes.Count, 1) : 1)
        : 1;

    /// <summary>Gets plain-language confirmation of what pressing the button will do.</summary>
    public string SvcOccurrenceSummary => SvcOccurrenceCount <= 1
        ? string.Empty
        : $"{SvcOccurrenceCount} serviços serão criados.";

    public bool HasSvcOccurrenceSummary => !string.IsNullOrEmpty(SvcOccurrenceSummary);

    public string SvcPrice { get; set; } = string.Empty;

    public string SvcPricePerDay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the percentage off the booking. Blank means full price.
    /// </summary>
    /// <remarks>
    /// Applies to every occurrence a repeat creates, which is the point: a standing discount for a
    /// regular tutor is agreed once, not per visit.
    /// </remarks>
    public string SvcDiscount { get; set; } = string.Empty;

    public bool SvcRequiresWalking { get; set; }

    public string SvcMsg { get; set; } = string.Empty;

    public bool HasSvcMsg => !string.IsNullOrEmpty(SvcMsg);

    public bool SvcMsgIsError { get; set; }

    private int DayCount => SvcUseDateRange
        ? Math.Max((SvcRepeatUntilPart.Date - SvcRangeFromPart.Date).Days + 1, 0)
        : SvcDates.Count;

    /// <summary>
    /// Clears the form and reloads its lists. Called from the View's OnLoaded, so reopening the
    /// tab always starts from a blank booking rather than whatever was typed last time.
    /// </summary>
    public async Task ReopenAsync()
    {
        ResetForm();
        SvcMsg = string.Empty;
        SvcMsgIsError = false;
        await ReloadDogsAsync().WithSync();
    }

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadDogsAsync()
    {
        var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        // Dogs carry only a TutorId, so the tutor names come from a second read and are joined
        // here — the same shape DogsViewModel uses to label its rows.
        var tutors = await repositoryTutors.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var tutorNames = tutors.ToDictionary(t => t.TutorId, t => t.Name);

        var previouslySelectedId = SelectedDog?.Id;

        DogOptions.Clear();
        foreach (var dog in dogs)
        {
            var tutorName = tutorNames.TryGetValue(dog.TutorId, out var name) ? name : string.Empty;
            DogOptions.Add(new DogOption(dog.DogId, dog.Name, dog.TutorId, tutorName));
        }

        SelectedDog = previouslySelectedId is int id ? DogOptions.FirstOrDefault(o => o.Id == id) : null;
        HasNoDogs = DogOptions.Count == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadDogsAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        return Task.CompletedTask;
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price);

    private void SetType(ServiceKind kind)
    {
        SvcType = kind;
        SvcMsg = string.Empty;
    }

    private async Task CreateService()
    {
        if (SelectedDog == null)
        {
            Fail("Selecione um cachorro.");
            return;
        }

        // Parsed before either branch: the field applies to every kind, and a bad rate should be
        // rejected before anything is written rather than per occurrence inside the loop.
        var discount = 0m;
        if (!string.IsNullOrWhiteSpace(SvcDiscount)
            && (!TryParsePrice(SvcDiscount, out discount) || discount < 0m || discount > 100m))
        {
            Fail("Informe um desconto entre 0 e 100%.");
            return;
        }

        Response result;

        if (SvcType == ServiceKind.Hotel)
        {
            if (!TryParsePrice(SvcPricePerDay, out var pricePerDay) || pricePerDay <= 0)
            {
                Fail("Informe um preço por dia válido.");
                return;
            }

            if (SvcEndDate <= SvcDate)
            {
                Fail("A saída deve ser depois da entrada.");
                return;
            }

            result = await repositoryServices.AddHotelAsync(new PetHotelService
            {
                DogId = SelectedDog.Id,
                PetSitterId = session.CurrentPetSitterId,
                StartDate = SvcDate,
                EndDate = SvcEndDate,
                PricePerDay = pricePerDay,
                Discount = discount,
                RequiresWalking = SvcRequiresWalking,
                ServicePaid = false,
                ServiceDone = false,
            }).WithSync();
        }
        else
        {
            if (!TryParsePrice(SvcPrice, out var price) || price <= 0)
            {
                Fail("Informe um preço válido.");
                return;
            }

            var occurrences = BuildOccurrences();
            if (occurrences.Count == 0)
            {
                Fail("A data final deve ser igual ou depois da inicial.");
                return;
            }

            // Each occurrence is written separately: the repositories take one service at a time,
            // and a partial failure should still leave the successful days scheduled.
            var created = 0;
            foreach (var occurrence in occurrences)
            {
                var occurrenceResult = SvcType switch
                {
                    ServiceKind.Sitting => await repositoryServices.AddSittingAsync(new PetSittingService
                    {
                        DogId = SelectedDog.Id,
                        PetSitterId = session.CurrentPetSitterId,
                        Date = occurrence,
                        Price = price,
                        Discount = discount,
                        ServicePaid = false,
                        ServiceDone = false,
                    }).WithSync(),

                    // Date only — BuildOccurrences already pinned day-care to midnight.
                    ServiceKind.DayCare => await repositoryServices.AddDayCareAsync(new DayCareService
                    {
                        DogId = SelectedDog.Id,
                        PetSitterId = session.CurrentPetSitterId,
                        Date = occurrence.Date,
                        Price = price,
                        Discount = discount,
                        RequiresWalking = SvcRequiresWalking,
                        ServicePaid = false,
                        ServiceDone = false,
                    }).WithSync(),

                    _ => await repositoryServices.AddWalkAsync(new WalkingService
                    {
                        DogId = SelectedDog.Id,
                        PetSitterId = session.CurrentPetSitterId,
                        Date = occurrence,
                        Price = price,
                        Discount = discount,
                        ServicePaid = false,
                        ServiceDone = false,
                    }).WithSync(),
                };

                if (occurrenceResult == Response.Successful)
                {
                    created++;
                }
            }

            if (created == 0)
            {
                Fail("Não foi possível agendar o serviço.");
                return;
            }

            // Credit is spent here, against the bookings just made — see CreditSpender.
            var creditNote = await creditSpender.SpendForDogAsync(session.CurrentPetSitterId, SelectedDog.Id).WithSync();

            ResetForm();
            SvcMsgIsError = created < occurrences.Count;
            SvcMsg = (created == occurrences.Count
                ? created == 1 ? "Serviço agendado." : $"{created} serviços agendados."
                : $"{created} de {occurrences.Count} serviços agendados.") + creditNote;

            session.NotifyDataChanged();
            return;
        }

        if (result != Response.Successful)
        {
            Fail("Não foi possível agendar o serviço.");
            return;
        }

        // Same as the recurring path: the hotel stay just booked is what the credit lands on.
        var hotelCreditNote = await creditSpender.SpendForDogAsync(session.CurrentPetSitterId, SelectedDog.Id).WithSync();

        ResetForm();
        SvcMsgIsError = false;
        SvcMsg = "Serviço agendado." + hotelCreditNote;

        session.NotifyDataChanged();
    }

    /// <summary>
    /// Every date-time the current settings should produce: each day in the repeat range crossed
    /// with each time of day. Without a repeat this is just the start date at each time.
    /// </summary>
    private List<DateTime> BuildOccurrences()
    {
        // Day-care has no time of day, so each chosen day yields exactly one booking at midnight
        // rather than one per time slot.
        var times = !SvcUsesTimes
            ? [TimeSpan.Zero]
            : SvcTimes.Count > 0
                ? SvcTimes.Select(t => t.Time).ToList()
                : [SvcTimePart];

        var days = SvcUseDateRange
            ? Enumerable.Range(0, DayCount).Select(offset => SvcRangeFromPart.Date.AddDays(offset))
            : SvcDates.Select(d => d.Date.Date).Distinct();

        var occurrences = new List<DateTime>();
        foreach (var day in days.OrderBy(d => d))
        {
            foreach (var time in times.OrderBy(t => t))
            {
                occurrences.Add(day + time);
            }
        }

        return occurrences;
    }

    private void AddDate()
    {
        // New rows continue from the latest day already listed, which is the usual intent when
        // picking a scattered set.
        var next = SvcDates.Count > 0
            ? SvcDates.Max(d => d.Date.Date).AddDays(1)
            : DateTime.Now.Date.AddDays(1);

        AddDateSlot(next);
    }

    private void AddDateSlot(DateTime date)
    {
        ServiceDateSlot? slot = null;
#pragma warning disable CA2000 // Ownership passes to SvcDates; disposed when the row is removed.
        var remove = new SynchronizedCommand(() => RemoveDateSlot(slot!), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
        slot = new ServiceDateSlot(date, remove);
        SvcDates.Add(slot);
        RefreshDateSlotState();
    }

    private void RemoveDateSlot(ServiceDateSlot slot)
    {
        if (SvcDates.Count <= 1)
        {
            return;
        }

        SvcDates.Remove(slot);
        (slot.RemoveCommand as IDisposable)?.Dispose();
        RefreshDateSlotState();
    }

    private void RefreshDateSlotState()
    {
        var removable = SvcDates.Count > 1;
        foreach (var slot in SvcDates)
        {
            slot.CanRemove = removable;
        }
    }

    private void ClearDateSlots()
    {
        foreach (var slot in SvcDates)
        {
            (slot.RemoveCommand as IDisposable)?.Dispose();
        }

        SvcDates.Clear();
    }

    /// <summary>
    /// Adds another time of day, an hour after the row above it.
    /// </summary>
    /// <remarks>
    /// Every added row used to arrive at 18:00, so entering a four-visit day meant retyping three
    /// of them. Consecutive hours are what a multi-visit booking usually looks like, and a row
    /// that starts close to right is quicker to correct than one that never is. Capped at 23:00
    /// rather than wrapping: a slot past midnight belongs to the next day, which this form has no
    /// way to express.
    /// </remarks>
    private void AddTime()
    {
        var next = SvcTimes.Count == 0
            ? FirstTimeOfDay
            : SvcTimes[^1].Time + TimeSpan.FromHours(1);

        AddTimeSlot(next > LastTimeOfDay ? LastTimeOfDay : next);
    }

    private void AddTimeSlot(TimeSpan time)
    {
        ServiceTimeSlot? slot = null;
#pragma warning disable CA2000 // Ownership passes to SvcTimes; disposed when the row is removed.
        var remove = new SynchronizedCommand(() => RemoveTimeSlot(slot!), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
        slot = new ServiceTimeSlot(time, remove);
        SvcTimes.Add(slot);
        RefreshTimeSlotState();
    }

    private void RemoveTimeSlot(ServiceTimeSlot slot)
    {
        // The form is meaningless with no time at all, so the last row is never removable.
        if (SvcTimes.Count <= 1)
        {
            return;
        }

        SvcTimes.Remove(slot);
        (slot.RemoveCommand as IDisposable)?.Dispose();
        RefreshTimeSlotState();
    }

    /// <summary>Keeps each row's delete affordance in step with how many rows remain.</summary>
    private void RefreshTimeSlotState()
    {
        var removable = SvcTimes.Count > 1;
        foreach (var slot in SvcTimes)
        {
            slot.CanRemove = removable;
        }
    }

    /// <summary>
    /// Returns the form to its opening state. Called after a successful save and on every open,
    /// so the screen never shows values left over from a previous booking.
    /// </summary>
    private void ResetForm()
    {
        SvcPrice = string.Empty;
        SvcPricePerDay = string.Empty;
        SvcDiscount = string.Empty;
        SvcRequiresWalking = false;
        SelectedDog = null;
        SvcUseDateRange = false;
        SvcDatePart = DateTime.Now.Date.AddDays(1);
        SvcRangeFromPart = DateTime.Now.Date.AddDays(1);
        SvcRepeatUntilPart = DateTime.Now.Date.AddDays(7);
        ClearDateSlots();
        AddDateSlot(DateTime.Now.Date.AddDays(1));
        SvcEndDatePart = DateTime.Now.Date.AddDays(3);
        SvcTimePart = FirstTimeOfDay;
        SvcEndTimePart = TimeSpan.FromHours(18);

        foreach (var slot in SvcTimes)
        {
            (slot.RemoveCommand as IDisposable)?.Dispose();
        }

        SvcTimes.Clear();
        AddTimeSlot(FirstTimeOfDay);
    }

    private void Fail(string message)
    {
        SvcMsgIsError = true;
        SvcMsg = message;
    }
}