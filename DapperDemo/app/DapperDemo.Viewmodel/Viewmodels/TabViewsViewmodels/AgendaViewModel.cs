using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.TabViewsViewmodels;

public enum HomeRangeFilter
{
    Hoje,
    Semana,
    Data,
}

[AddINotifyPropertyChangedInterface]
public class AgendaViewModel : PresentationModelBase<Unit, Unit>
{
    /// <summary>The month number standing for "no month filter, just the year".</summary>
    private const int WholeYear = 0;

    private static readonly string[] MonthsShort = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    private static readonly CultureInfo Brazil = new("pt-BR");

    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;
    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;
    private readonly CurrentView currentView;

    /// <summary>
    /// Guards the year hook while <see cref="ReloadAsync"/> rebuilds that list. Assigning
    /// SelectedYear there would otherwise re-enter ReloadAsync through Fody's OnXChanged hook.
    /// The month list is fixed, so it needs no such guard.
    /// </summary>
    private bool rebuildingOptions;

    public AgendaViewModel(
        CurrentView currentView,
        RepositoryServices repositoryServices,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryServices = repositoryServices;
        this.session = session;
        serviceDetailView = serviceDetailFactory.Create();
        this.currentView = currentView;

        // Settling a payment from the tutor screen, or toggling done on the service screen, must
        // show up here on return. This view-model outlives a single OnRunStarting (MainViewModel
        // builds all five tabs once).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        SetRangeHoje = new SynchronizedCommand(
            () =>
        {
            var now = DateTime.Now;
            SelectedMonth = MonthOptions.First(m => m.Number == now.Month);
            SetRange(HomeRangeFilter.Hoje);
        },
            SynchronizationBehavior.Discard,
            true);
        SetRangeSemana = new SynchronizedCommand(
            () =>
        {
            var now = DateTime.Now;
            SelectedMonth = MonthOptions.First(m => m.Number == now.Month);
            SetRange(HomeRangeFilter.Semana);
        },
            SynchronizationBehavior.Discard,
            true);

        SetRangeData = new SynchronizedCommand(
            () =>
        {
            SelectedMonth = MonthOptions[0];
            SetRange(HomeRangeFilter.Data);
        },
            SynchronizationBehavior.Discard,
            true);

        SetTypeTodos = new SynchronizedCommand(() => SetType(null), SynchronizationBehavior.Discard, true);
        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);
        SetTypeDayCare = new SynchronizedCommand(() => SetType(ServiceKind.DayCare), SynchronizationBehavior.Discard, true);
        PreviousPeriodCommand = new SynchronizedCommand(() => StepPeriod(-1), SynchronizationBehavior.Discard, true);
        NextPeriodCommand = new SynchronizedCommand(() => StepPeriod(1), SynchronizationBehavior.Discard, true);

        TodayLabel = FormatToday();
        HomeRange = HomeRangeFilter.Semana;

        // Fixed list: the twelve months, plus a whole-year entry so "Data" can still scope to a
        // year without a separate chip for it.
        MonthOptions.Add(new MonthOption(WholeYear, "Ano todo"));
        for (var month = 1; month <= 12; month++)
        {
            MonthOptions.Add(new MonthOption(month, MonthName(month)));
        }

        var now = DateTime.Now;
        SelectedMonth = MonthOptions.First(m => m.Number == now.Month);
        SelectedYear = now.Year;
    }

    public ICommand SetRangeHoje { get; }

    public ICommand SetRangeSemana { get; }

    public ICommand SetRangeData { get; }

    public ICommand SetTypeTodos { get; }

    public ICommand SetTypeWalk { get; }

    public ICommand SetTypeSitting { get; }

    public ICommand SetTypeHotel { get; }

    public ICommand SetTypeDayCare { get; }

    public string TodayLabel { get; private set; }

    public HomeRangeFilter HomeRange { get; private set; }

    public ServiceKind? HomeType { get; private set; }

    /// <summary>Gets or sets a value indicating whether two-way bound to the "incluir pagos" checkbox; Fody calls OnHomeShowPaidChanged on every change.</summary>
    public bool HomeShowPaid { get; set; }

    /// <summary>Gets or sets the month shown while <see cref="HomeRangeFilter.Data"/> is active.</summary>
    public MonthOption? SelectedMonth { get; set; }

    /// <summary>Gets or sets the year shown while <see cref="HomeRangeFilter.Data"/> is active.</summary>
    public int SelectedYear { get; set; }

    /// <summary>Gets the twelve months plus the whole-year entry. Fixed, so it never rebuilds.</summary>
    public ObservableCollection<MonthOption> MonthOptions { get; } = [];

    /// <summary>Gets the years that actually have services, most recent first.</summary>
    public ObservableCollection<int> YearOptions { get; } = [];

    /// <summary>
    /// Gets the period as one line, e.g. "Agosto 2026" or "Ano todo de 2026".
    /// </summary>
    /// <remarks>
    /// Replaces the two drop-downs. A popup lays out in its own visual root and so ignores the
    /// design canvas' scale — at phone size the list came out several times wider than the control
    /// that opened it. Stepping keeps the whole interaction inside ordinary layout.
    /// </remarks>
    public string PeriodLabel => SelectedMonth is not { } month || month.Number == ServicePeriod.WholeYear
        ? $"Ano todo de {SelectedYear.ToString(CultureInfo.InvariantCulture)}"
        : $"{month.Label} {SelectedYear.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Gets the command stepping the period back one month.</summary>
    public ICommand PreviousPeriodCommand { get; private set; } = null!;

    /// <summary>Gets the command stepping the period forward one month.</summary>
    public ICommand NextPeriodCommand { get; private set; } = null!;

    public bool IsRangeHoje => HomeRange == HomeRangeFilter.Hoje;

    public bool IsRangeSemana => HomeRange == HomeRangeFilter.Semana;

    public bool IsRangeData => HomeRange == HomeRangeFilter.Data;

    /// <summary>Gets a value indicating whether the month and year dropdowns are revealed under the chips.</summary>
    public bool ShowDatePicker => HomeRange == HomeRangeFilter.Data;

    public bool IsTypeTodos => HomeType == null;

    public bool IsTypeWalk => HomeType == ServiceKind.Walk;

    public bool IsTypeSitting => HomeType == ServiceKind.Sitting;

    public bool IsTypeHotel => HomeType == ServiceKind.Hotel;

    public bool IsTypeDayCare => HomeType == ServiceKind.DayCare;

    public bool HasNoServices { get; private set; } = true;

    /// <summary>Gets a value indicating whether true only when the account has no services at all, versus none matching the filters.</summary>
    public bool HasNothingBooked { get; private set; }

    /// <summary>
    /// Gets the dogs with services under the current filters, each collapsed until tapped.
    /// </summary>
    /// <remarks>
    /// The only shape the agenda has. "Mostrar pagos" changes which services are in scope, not how
    /// they are presented — a flat list and a grouped one behaving differently for the same data
    /// was the part that made the screen hard to read.
    /// </remarks>
    public ObservableCollection<DogServiceGroup> DogGroups { get; } = [];

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        var all = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        RebuildPeriodOptions(all);

        var filtered = all.Where(Matches).OrderBy(sv => sv.Date).ToArray();

        // Ticking Paid or Feito writes, which raises DataChanged, which lands back here — so a
        // reload happens while the user is looking at an open dog. The groups are rebuilt from
        // scratch each time, so which ones were open has to be carried across or the list would
        // snap shut under them.
        var expanded = DogGroups
            .Where(group => group.IsExpanded)
            .Select(group => group.DogName)
            .ToHashSet(StringComparer.Ordinal);

        ClearRows();
        BuildDogGroups(filtered, expanded);

        HasNoServices = filtered.Length == 0;
        HasNothingBooked = all.Length == 0;
    }

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        ClearRows();
        return Task.CompletedTask;
    }

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever HomeShowPaid changes.</summary>
    protected void OnHomeShowPaidChanged() => AppSession.FireAndForget(ReloadAsync());

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedMonth changes.</summary>
    protected void OnSelectedMonthChanged()
    {
        if (!rebuildingOptions)
        {
            AppSession.FireAndForget(ReloadAsync());
        }
    }

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever SelectedYear changes.</summary>
    protected void OnSelectedYearChanged()
    {
        if (!rebuildingOptions)
        {
            AppSession.FireAndForget(ReloadAsync());
        }
    }

    private static string FormatToday() => DateTime.Now.ToString("dddd, dd 'de' MMMM", Brazil);

    /// <summary>"Março", capitalised the way the rest of the UI writes month names.</summary>
    private static string MonthName(int month)
    {
        var name = Brazil.DateTimeFormat.GetMonthName(month);
        return char.ToUpper(name[0], Brazil) + name[1..];
    }

    private bool Matches(ServiceItem service)
    {
        var now = DateTime.Now;

        switch (HomeRange)
        {
            case HomeRangeFilter.Hoje when service.Date.Date != now.Date:
                return false;

            case HomeRangeFilter.Semana when service.Date < now.Date || service.Date >= now.Date.AddDays(7):
                return false;

            // "Ano todo" keeps the year but drops the month, so one picker covers both a single
            // month and a whole year.
            case HomeRangeFilter.Data when service.Date.Year != SelectedYear:
                return false;

            case HomeRangeFilter.Data when SelectedMonth is { Number: not WholeYear } month
                && service.Date.Month != month.Number:
                return false;

            default:
                break;
        }

        if (HomeType != null && service.Kind != HomeType)
        {
            return false;
        }

        return HomeShowPaid || !service.ServiceDone;
    }

    /// <summary>
    /// Rebuilds the year dropdown from the years that actually have bookings, always including the
    /// current one so the picker is never empty on a fresh account.
    /// </summary>
    private void RebuildPeriodOptions(ServiceItem[] all)
    {
        var years = all
            .Select(s => s.Date.Year)
            .Append(DateTime.Now.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToArray();

        if (YearOptions.SequenceEqual(years))
        {
            return;
        }

        rebuildingOptions = true;
        try
        {
            var keptYear = years.Contains(SelectedYear) ? SelectedYear : years[0];

            YearOptions.Clear();
            foreach (var year in years)
            {
                YearOptions.Add(year);
            }

            SelectedYear = keptYear;
        }
        finally
        {
            rebuildingOptions = false;
        }
    }

    /// <summary>
    /// Groups the filtered services under their dog, collapsed until tapped.
    /// </summary>
    /// <remarks>
    /// Every filter still applies inside a group — the period chips, the type chips and the paid
    /// checkbox all narrow what a dog expands to. Dogs are alphabetical, and their services keep
    /// the list's own newest-first order.
    /// </remarks>
    /// <param name="filtered">The services in scope, already narrowed by every filter.</param>
    /// <param name="expanded">
    /// Dogs that were open before the rebuild, by name. Name rather than id because that is what
    /// the grouping keys on, and it survives a service being added or removed underneath.
    /// </param>
    private void BuildDogGroups(ServiceItem[] filtered, HashSet<string> expanded)
    {
        var byDog = filtered
            .GroupBy(s => s.DogName, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture);

        foreach (var dog in byDog)
        {
            var services = dog.Select(CreateRow).ToArray();
            var count = services.Length == 1 ? "1 serviço" : $"{services.Length} serviços";

            // What the group is worth in full, discounts included — Total is the figure every
            // other balance in the app is built from.
            var total = AppSession.Money(dog.Sum(s => s.Total));

            DogGroups.Add(new DogServiceGroup(dog.Key, count, total, services)
            {
                IsExpanded = expanded.Contains(dog.Key),
            });
        }
    }

    /// <summary>
    /// Moves the period by whole months, carrying into the next or previous year at the ends.
    /// </summary>
    /// <remarks>
    /// "Ano todo" is a peer of the twelve rather than a thirteenth step, so stepping off it lands
    /// on a real month — January going forward, December going back — instead of cycling through a
    /// state the arrows cannot express.
    /// </remarks>
    /// <param name="delta">−1 or +1.</param>
    private void StepPeriod(int delta)
    {
        var current = SelectedMonth?.Number ?? DateTime.Now.Month;
        if (current == ServicePeriod.WholeYear)
        {
            current = delta > 0 ? 0 : 13;
        }

        var next = current + delta;
        var year = SelectedYear;

        if (next < 1)
        {
            next = 12;
            year -= 1;
        }
        else if (next > 12)
        {
            next = 1;
            year += 1;
        }

        // The year first: both assignments raise PropertyChanged and the reload hook reads both,
        // so setting the month last means the rebuild sees the pair it is meant to.
        if (!YearOptions.Contains(year))
        {
            YearOptions.Add(year);
        }

        SelectedYear = year;
        SelectedMonth = MonthOptions.FirstOrDefault(m => m.Number == next) ?? SelectedMonth;
    }

    /// <summary>Builds one agenda row. Shared so a grouped row behaves exactly like a flat one.</summary>
    private ServiceRow CreateRow(ServiceItem sv)
    {
        var priceLabel = sv.Kind == ServiceKind.Hotel
            ? AppSession.Money(sv.Price) + " / dia"
            : AppSession.Money(sv.Price);

        // CA2000: ownership passes to the ServiceRow below, which disposes the command when this
        // list is rebuilt (see ClearRows). Paid/done tags are display-only on the agenda.
#pragma warning disable CA2000
        var openCommand = new SynchronizedCommand(() => Open(sv.Kind, sv.ServiceId), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000

        return new ServiceRow(
            sv.Date.Day.ToString("00", CultureInfo.InvariantCulture),
            MonthsShort[sv.Date.Month - 1],
            sv.DogName,
            AppSession.TypeLabel(sv.Kind),
            AppSession.TimeLabel(sv.Date, sv.Kind),
            priceLabel,
            sv.ServicePaid,
            sv.ServicePaid ? "Pago" : "Sem pagar",
            sv.ServiceDone,
            sv.ServiceDone ? "Feito" : "A fazer",
            openCommand);
    }

    private void SetRange(HomeRangeFilter range)
    {
        HomeRange = range;
        AppSession.FireAndForget(ReloadAsync());
    }

    private void SetType(ServiceKind? kind)
    {
        HomeType = kind;
        AppSession.FireAndForget(ReloadAsync());
    }

    private void ClearRows()
    {
        // Disposing a group disposes the rows inside it, and each row its open command.
        foreach (var group in DogGroups)
        {
            group.Dispose();
        }

        DogGroups.Clear();
    }

    private Task Open(ServiceKind kind, int serviceId)
    {
        session.SelectedServiceKind = kind;
        session.SelectedServiceId = serviceId;
        currentView.ViewShown = serviceDetailView;
        return Task.CompletedTask;
    }
}