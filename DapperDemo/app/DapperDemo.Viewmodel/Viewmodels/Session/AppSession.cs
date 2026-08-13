using DapperDemo.Repository.Dapper.Dtos;
using System.Globalization;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

/// <summary>
/// Who is logged in, which row a pushed detail screen should show, and a notification that the
/// database changed. Registered as a DI singleton because every tab needs the same answer.
/// Holds no domain data of its own — all dogs, tutors and services come from the repositories.
/// </summary>
public class AppSession
{
    /// <summary>
    /// Raised after any write. MainViewModel builds all five tab presenters once, so OnRunStarting
    /// does not run again on a tab switch — without this, a record added on one tab would not
    /// appear on another until relaunch.
    /// </summary>
    public event EventHandler? DataChanged;

    public event EventHandler? LogoutRequested;

    public int CurrentPetSitterId { get; private set; }

    public string CurrentUserName { get; private set; } = string.Empty;

    public bool IsLoggedIn => CurrentPetSitterId > 0;

    // Detail screens are pushed via Factory<PresenterBase<T,Unit,Unit>>.Create(), which takes no
    // runtime arguments, so the tapped row is handed over here instead.
    public int? SelectedDogId { get; set; }

    public int? SelectedTutorId { get; set; }

    public ServiceKind? SelectedServiceKind { get; set; }

    public int? SelectedServiceId { get; set; }

    /// <summary>
    /// Starts a reload without awaiting it, but reports failures. A bare <c>_ = SomeAsync()</c>
    /// swallows the exception, which turns a broken query into a screen that is silently blank
    /// with no clue why.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "Not a notification — this observes a task that is intentionally not awaited.")]
    public static void FireAndForget(Task task) =>
        task.ContinueWith(
            t => Console.WriteLine($"[DapperDemo] background reload failed: {t.Exception?.GetBaseException()}"),
            TaskContinuationOptions.OnlyOnFaulted);

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0])));

    public static string TypeLabel(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => "Passeio",
        ServiceKind.Sitting => "Pet sitting",
        ServiceKind.Hotel => "Hotel",
        ServiceKind.DayCare => "Day-Care",
        _ => string.Empty,
    };

    /// <summary>Brazilian currency shape (R$ 0,00), independent of the host machine's locale.</summary>
    public static string Money(decimal value) => "R$ " + value.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Date shape used across the app's detail screens (dd/MM/yyyy, HH:mm).</summary>
    public static string DateTimeLabel(DateTime value) => value.ToString("dd/MM/yyyy, HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// The same label, but without the clock for kinds that have no time of day. Day-care is
    /// stored at midnight, so the plain overload would render every booking as ", 00:00".
    /// </summary>
    public static string DateTimeLabel(DateTime value, ServiceKind kind) => kind == ServiceKind.DayCare
        ? value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        : DateTimeLabel(value);

    /// <summary>
    /// Where a hotel stay ends, phrased as a continuation of where it starts ("até 07/08/2026,
    /// 10:00"). Empty for every other kind, none of which has a check-out.
    /// </summary>
    /// <param name="service">The service being described.</param>
    /// <returns>The check-out line, or an empty string.</returns>
    public static string StayEndLabel(ServiceItem service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return service.Kind == ServiceKind.Hotel && service.EndDate is DateTime end
            ? $"até {DateTimeLabel(end)}"
            : string.Empty;
    }

    /// <summary>
    /// How a hotel stay's total is arrived at: the nights it covers times the daily rate, and the
    /// one-off extra on a second line when there is one.
    /// </summary>
    /// <remarks>
    /// A stay is entered as a daily rate, so its total is a figure nobody typed — printed on its
    /// own it invites the tutor to ask where it came from. Empty for every other kind, whose price
    /// is already the whole story.
    /// </remarks>
    /// <param name="service">The service being described.</param>
    /// <returns>The breakdown lines separated by <c>\n</c>, or an empty string.</returns>
    public static string StayPriceBreakdown(ServiceItem service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (service.Kind != ServiceKind.Hotel)
        {
            return string.Empty;
        }

        var nights = service.Nights;
        var rate = $"{nights.ToString(CultureInfo.InvariantCulture)} {(nights == 1 ? "diária" : "diárias")} × {Money(service.Price)}";

        return service.ExtraCharge > 0m
            ? $"{rate}\n+ {Money(service.ExtraCharge)} adicional"
            : rate;
    }

    /// <summary>
    /// What the agenda prints in its time column. Day-care occupies the whole day rather than a
    /// slot, so it says so instead of showing midnight.
    /// </summary>
    public static string TimeLabel(DateTime value, ServiceKind kind) => kind == ServiceKind.DayCare
        ? "Dia todo"
        : value.ToString("HH:mm", CultureInfo.InvariantCulture);

    public void SignIn(PetSitter petSitter)
    {
        CurrentPetSitterId = petSitter.PetSitterId;
        CurrentUserName = petSitter.Name;
    }

    public void SignOut()
    {
        CurrentPetSitterId = 0;
        CurrentUserName = string.Empty;
        SelectedDogId = null;
        SelectedTutorId = null;
        SelectedServiceKind = null;
        SelectedServiceId = null;
    }

    public void NotifyDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);

    public void RequestLogout() => LogoutRequested?.Invoke(this, EventArgs.Empty);
}