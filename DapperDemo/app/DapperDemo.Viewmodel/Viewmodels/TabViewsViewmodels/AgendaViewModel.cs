using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;
using DapperDemo.Viewmodel.Viewmodels.Session;
using DapperDemo.Viewmodel.Viewmodels.Utils;
using PropertyChanged;

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
    private readonly CreditSpender creditSpender;
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
        CreditSpender creditSpender,
        AppSession session,
        Factory<PresenterBase<ServiceDetailViewModel, Unit, Unit>> serviceDetailFactory)
    {
        this.repositoryServices = repositoryServices;
        this.creditSpender = creditSpender;
        this.session = session;
        serviceDetailView = serviceDetailFactory.Create();
        this.currentView = currentView;

        // Marking a service paid from the detail screen must show up here on return, and this
        // view-model outlives a single OnRunStarting (MainViewModel builds all five tabs once).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        SetRangeHoje = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Hoje), SynchronizationBehavior.Discard, true);
        SetRangeSemana = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Semana), SynchronizationBehavior.Discard, true);
        SetRangeData = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Data), SynchronizationBehavior.Discard, true);
        SetTypeTodos = new SynchronizedCommand(() => SetType(null), SynchronizationBehavior.Discard, true);
        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);
        SetTypeDayCare = new SynchronizedCommand(() => SetType(ServiceKind.DayCare), SynchronizationBehavior.Discard, true);

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
    /// Gets a value indicating whether the per-dog summary replaces the chronological list. Turning
    /// on "incluir pagos" brings in every past booking, which is what makes a flat list unreadable.
    /// </summary>
    public bool ShowSummary => HomeShowPaid;

    public bool ShowServiceList => !HomeShowPaid;

    public ObservableCollection<ServiceRow> FilteredServices { get; } = [];

    public ObservableCollection<DogSummaryRow> DogSummaries { get; } = [];

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        var all = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        RebuildPeriodOptions(all);

        var filtered = all.Where(Matches).OrderBy(sv => sv.Date).ToArray();

        ClearRows();
        BuildServiceRows(filtered);
        BuildDogSummaries(filtered);

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

        return HomeShowPaid || !service.ServicePaid;
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

    private void BuildServiceRows(ServiceItem[] filtered)
    {
        foreach (var sv in filtered)
        {
            var priceLabel = sv.Kind == ServiceKind.Hotel
                ? AppSession.Money(sv.Price) + " / dia"
                : AppSession.Money(sv.Price);

            // CA2000: ownership passes to the ServiceRow below, which disposes every command
            // when this list is rebuilt (see ClearRows).
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(() => Open(sv.Kind, sv.ServiceId), SynchronizationBehavior.Discard, true);
            var toggleCommand = new SynchronizedCommand(() => TogglePaid(sv.Kind, sv.ServiceId, !sv.ServicePaid), SynchronizationBehavior.Discard, true);
            var toggleDoneCommand = new SynchronizedCommand(() => ToggleDone(sv.Kind, sv.ServiceId, sv.DogId, !sv.ServiceDone), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000

            FilteredServices.Add(new ServiceRow(
                sv.Date.Day.ToString("00", CultureInfo.InvariantCulture),
                MonthsShort[sv.Date.Month - 1],
                sv.DogName,
                AppSession.TypeLabel(sv.Kind),
                AppSession.TimeLabel(sv.Date, sv.Kind),
                priceLabel,
                sv.ServicePaid,
                sv.ServicePaid ? "Pago" : "Pendente",
                sv.ServiceDone,
                sv.ServiceDone ? "Feito" : "A fazer",
                sv.ServiceDone || sv.ServicePaid,
                openCommand,
                toggleCommand,
                toggleDoneCommand));
        }
    }

    /// <summary>
    /// Collapses the filtered services into one card per dog: each service type with how many of
    /// them and what they came to, plus the dog's total for the timeframe. A hotel stay is billed
    /// per day, so its contribution is the daily rate times the nights booked.
    /// </summary>
    private void BuildDogSummaries(ServiceItem[] filtered)
    {
        foreach (var row in DogSummaryBuilder.Build(filtered, AppSession.Money))
        {
            DogSummaries.Add(row);
        }
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
        foreach (var row in FilteredServices)
        {
            row.Dispose();
        }

        FilteredServices.Clear();
        DogSummaries.Clear();
    }

    private async Task TogglePaid(ServiceKind kind, int serviceId, bool paid)
    {
        await repositoryServices.SetPaidAsync(kind, serviceId, paid).WithSync();
        session.NotifyDataChanged();
    }

    /// <summary>
    /// Ticks a booking off as carried out, which is the day's work rather than the month's billing.
    /// </summary>
    /// <remarks>
    /// Carrying the work out is what makes it billable, so this is also when any credit the tutor
    /// is holding gets spent against it — the same rule the service screen follows.
    /// </remarks>
    private async Task ToggleDone(ServiceKind kind, int serviceId, int dogId, bool done)
    {
        await repositoryServices.SetDoneAsync(kind, serviceId, done).WithSync();

        if (done)
        {
            await creditSpender.SpendForDogAsync(session.CurrentPetSitterId, dogId).WithSync();
        }

        session.NotifyDataChanged();
    }

    private Task Open(ServiceKind kind, int serviceId)
    {
        session.SelectedServiceKind = kind;
        session.SelectedServiceId = serviceId;
        currentView.ViewShown = serviceDetailView;
        return Task.CompletedTask;
    }
}