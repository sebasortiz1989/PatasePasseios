using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public enum HomeRangeFilter
{
    Hoje,
    Semana,
    Todos
}

/// <summary>
/// One row of the agenda. Owns its two commands, so the list can dispose them when the
/// filters rebuild it rather than leaking a pair per row on every filter change.
/// </summary>
public sealed class ServiceRow(string dayNum, string monthShort, string dogName, string typeLabel, string timeLabel, string priceLabel, bool paid, string paidLabel, ICommand openCommand, ICommand toggleCommand) : IDisposable
{
    public string DayNum { get; } = dayNum;

    public string MonthShort { get; } = monthShort;

    public string DogName { get; } = dogName;

    public string TypeLabel { get; } = typeLabel;

    public string TimeLabel { get; } = timeLabel;

    public string PriceLabel { get; } = priceLabel;

    public bool Paid { get; } = paid;

    public string PaidLabel { get; } = paidLabel;

    public ICommand OpenCommand { get; } = openCommand;

    public ICommand ToggleCommand { get; } = toggleCommand;

    public void Dispose()
    {
        (OpenCommand as IDisposable)?.Dispose();
        (ToggleCommand as IDisposable)?.Dispose();
    }
}

[AddINotifyPropertyChangedInterface]
public class AgendaViewModel : PresentationModelBase<Unit, Unit>
{
    private static readonly string[] MonthsShort = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;
    private readonly PresenterBase<ServiceDetailViewModel, Unit, Unit> serviceDetailView;
    private readonly CurrentView currentView;

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

        // Marking a service paid from the detail screen must show up here on return, and this
        // view-model outlives a single OnRunStarting (MainViewModel builds all five tabs once).
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadAsync());
        session.DataChanged += dataChangedHandler;

        SetRangeHoje = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Hoje), SynchronizationBehavior.Discard, true);
        SetRangeSemana = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Semana), SynchronizationBehavior.Discard, true);
        SetRangeTodos = new SynchronizedCommand(() => SetRange(HomeRangeFilter.Todos), SynchronizationBehavior.Discard, true);
        SetTypeTodos = new SynchronizedCommand(() => SetType(null), SynchronizationBehavior.Discard, true);
        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);

        TodayLabel = FormatToday();
        HomeRange = HomeRangeFilter.Semana;
    }

    public ICommand SetRangeHoje { get; }

    public ICommand SetRangeSemana { get; }

    public ICommand SetRangeTodos { get; }

    public ICommand SetTypeTodos { get; }

    public ICommand SetTypeWalk { get; }

    public ICommand SetTypeSitting { get; }

    public ICommand SetTypeHotel { get; }

    public string TodayLabel { get; private set; }

    public HomeRangeFilter HomeRange { get; private set; }

    public ServiceKind? HomeType { get; private set; }

    /// <summary>Two-way bound to the "incluir pagos" checkbox; Fody calls OnHomeShowPaidChanged on every change.</summary>
    public bool HomeShowPaid { get; set; }

    public bool IsRangeHoje => HomeRange == HomeRangeFilter.Hoje;

    public bool IsRangeSemana => HomeRange == HomeRangeFilter.Semana;

    public bool IsRangeTodos => HomeRange == HomeRangeFilter.Todos;

    public bool IsTypeTodos => HomeType == null;

    public bool IsTypeWalk => HomeType == ServiceKind.Walk;

    public bool IsTypeSitting => HomeType == ServiceKind.Sitting;

    public bool IsTypeHotel => HomeType == ServiceKind.Hotel;

    public bool HasNoServices { get; private set; } = true;

    /// <summary>True only when the account has no services at all, versus none matching the filters.</summary>
    public bool HasNothingBooked { get; private set; }

    public ObservableCollection<ServiceRow> FilteredServices { get; } = [];

    protected override async Task OnRunStarting(Unit input) => await ReloadAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        ClearRows();
        return Task.CompletedTask;
    }

    /// <summary>PropertyChanged.Fody convention hook — invoked whenever HomeShowPaid changes.</summary>
    protected void OnHomeShowPaidChanged() => AppSession.FireAndForget(ReloadAsync());

    private static string FormatToday()
    {
        var culture = new CultureInfo("pt-BR");
        return DateTime.Now.ToString("dddd, dd 'de' MMMM", culture);
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

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadAsync()
    {
        var all = await repositoryServices.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();

        var now = DateTime.Now;
        var startToday = now.Date;
        var weekEnd = startToday.AddDays(7);

        var filtered = all.Where(sv =>
        {
            if (HomeRange == HomeRangeFilter.Hoje && sv.Date.Date != now.Date)
            {
                return false;
            }

            if (HomeRange == HomeRangeFilter.Semana && (sv.Date < startToday || sv.Date >= weekEnd))
            {
                return false;
            }

            if (HomeType != null && sv.Kind != HomeType)
            {
                return false;
            }

            if (!HomeShowPaid && sv.ServicePaid)
            {
                return false;
            }

            return true;
        }).OrderBy(sv => sv.Date).ToArray();

        ClearRows();
        foreach (var sv in filtered)
        {
            var priceLabel = sv.Kind == ServiceKind.Hotel
                ? AppSession.Money(sv.Price) + " / dia"
                : AppSession.Money(sv.Price);

            // CA2000: ownership passes to the ServiceRow below, which disposes both commands
            // when this list is rebuilt (see ClearRows).
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(() => Open(sv.Kind, sv.ServiceId), SynchronizationBehavior.Discard, true);
            var toggleCommand = new SynchronizedCommand(() => TogglePaid(sv.Kind, sv.ServiceId, !sv.ServicePaid), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000

            FilteredServices.Add(new ServiceRow(
                sv.Date.Day.ToString("00", CultureInfo.InvariantCulture),
                MonthsShort[sv.Date.Month - 1],
                sv.DogName,
                AppSession.TypeLabel(sv.Kind),
                sv.Date.ToString("HH:mm", CultureInfo.InvariantCulture),
                priceLabel,
                sv.ServicePaid,
                sv.ServicePaid ? "Pago" : "Pendente",
                openCommand,
                toggleCommand));
        }

        HasNoServices = filtered.Length == 0;
        HasNothingBooked = all.Length == 0;
    }

    private void ClearRows()
    {
        foreach (var row in FilteredServices)
        {
            row.Dispose();
        }

        FilteredServices.Clear();
    }

    private async Task TogglePaid(ServiceKind kind, int serviceId, bool paid)
    {
        await repositoryServices.SetPaidAsync(kind, serviceId, paid).WithSync();
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
