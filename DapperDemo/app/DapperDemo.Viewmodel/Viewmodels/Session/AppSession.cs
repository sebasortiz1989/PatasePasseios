using DapperDemo.Mensagens.Dapper.Dtos;
using System.Globalization;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

/// <summary>
/// Who is logged in, which row a pushed detail screen should show, and a notification that the
/// database changed. Registered as a DI singleton because every tab needs the same answer.
/// Holds no domain data of its own — all dogs, tutors and services come from the repositories.
/// </summary>
public class AppSession
{
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
    /// Raised after any write. MainViewModel builds all five tab presenters once, so OnRunStarting
    /// does not run again on a tab switch — without this, a record added on one tab would not
    /// appear on another until relaunch.
    /// </summary>
    public event EventHandler? DataChanged;

    public event EventHandler? LogoutRequested;

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

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0])));

    public static string TypeLabel(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => "Passeio",
        ServiceKind.Sitting => "Pet sitting",
        ServiceKind.Hotel => "Hotel",
        _ => string.Empty,
    };

    /// <summary>Brazilian currency shape (R$ 0,00), independent of the host machine's locale.</summary>
    public static string Money(decimal value) => "R$ " + value.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Date shape used across the app's detail screens (dd/MM/yyyy, HH:mm).</summary>
    public static string DateTimeLabel(DateTime value) => value.ToString("dd/MM/yyyy, HH:mm", CultureInfo.InvariantCulture);
}