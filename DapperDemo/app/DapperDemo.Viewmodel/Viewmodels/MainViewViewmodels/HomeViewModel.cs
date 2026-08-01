using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public enum HomeRangeFilter
{
    Hoje,
    Semana,
    Todos
}

public class ServiceRow(string dayNum, string monthShort, string dogName, string typeLabel, string timeLabel, string priceLabel, bool paid, string paidLabel, ICommand openCommand, ICommand toggleCommand)
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
}

[AddINotifyPropertyChangedInterface]
public class HomeViewModel : PresentationModelBase<Void, Void>
{
    private static readonly string[] MonthsShort = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    private readonly NavigationController navigationController;
    private readonly MockAppData mockAppData;
    private readonly Factory<PresenterBase<ServiceDetailViewModel, Void, Void>> serviceDetailFactory;

    public HomeViewModel(
        NavigationController navigationController,
        MockAppData mockAppData,
        Factory<PresenterBase<ServiceDetailViewModel, Void, Void>> serviceDetailFactory)
    {
        this.navigationController = navigationController;
        this.mockAppData = mockAppData;
        this.serviceDetailFactory = serviceDetailFactory;
        mockAppData.DataChanged += Refresh;

        SetRangeHoje = new SynchronizedCommand(() => { HomeRange = HomeRangeFilter.Hoje; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetRangeSemana = new SynchronizedCommand(() => { HomeRange = HomeRangeFilter.Semana; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetRangeTodos = new SynchronizedCommand(() => { HomeRange = HomeRangeFilter.Todos; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetTypeTodos = new SynchronizedCommand(() => { HomeType = null; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetTypeWalk = new SynchronizedCommand(() => { HomeType = ServiceKind.Walk; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => { HomeType = ServiceKind.Sitting; Refresh(); }, SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => { HomeType = ServiceKind.Hotel; Refresh(); }, SynchronizationBehavior.Discard, true);
        ToggleShowPaidCommand = new SynchronizedCommand(() => { HomeShowPaid = !HomeShowPaid; Refresh(); }, SynchronizationBehavior.Discard, true);

        TodayLabel = FormatToday();
        HomeRange = HomeRangeFilter.Semana;
        Refresh();
    }

    public ICommand SetRangeHoje { get; }

    public ICommand SetRangeSemana { get; }

    public ICommand SetRangeTodos { get; }

    public ICommand SetTypeTodos { get; }

    public ICommand SetTypeWalk { get; }

    public ICommand SetTypeSitting { get; }

    public ICommand SetTypeHotel { get; }

    public ICommand ToggleShowPaidCommand { get; }

    public string TodayLabel { get; private set; }

    public HomeRangeFilter HomeRange { get; private set; }

    public ServiceKind? HomeType { get; private set; }

    public bool HomeShowPaid { get; private set; }

    public bool IsRangeHoje => HomeRange == HomeRangeFilter.Hoje;

    public bool IsRangeSemana => HomeRange == HomeRangeFilter.Semana;

    public bool IsRangeTodos => HomeRange == HomeRangeFilter.Todos;

    public bool IsTypeTodos => HomeType == null;

    public bool IsTypeWalk => HomeType == ServiceKind.Walk;

    public bool IsTypeSitting => HomeType == ServiceKind.Sitting;

    public bool IsTypeHotel => HomeType == ServiceKind.Hotel;

    public bool HasNoServices { get; private set; }

    public ObservableCollection<ServiceRow> FilteredServices { get; } = [];

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private static string FormatToday()
    {
        var culture = new CultureInfo("pt-BR");
        return DateTime.Now.ToString("dddd, dd 'de' MMMM", culture);
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        var startToday = now.Date;
        var weekEnd = startToday.AddDays(7);

        var filtered = mockAppData.Services.Where(sv =>
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

            if (!HomeShowPaid && sv.Paid)
            {
                return false;
            }

            return true;
        }).OrderBy(sv => sv.Date).ToList();

        FilteredServices.Clear();
        foreach (var sv in filtered)
        {
            var dog = mockAppData.DogById(sv.DogId);
            var priceLabel = sv.Kind == ServiceKind.Hotel
                ? MockAppData.Money(sv.PricePerDay ?? 0) + " / dia"
                : MockAppData.Money(sv.Price ?? 0);
            var openCommand = new SynchronizedCommand(() => Open(sv.Id), SynchronizationBehavior.Discard, true);
            var toggleCommand = new SynchronizedCommand(() => mockAppData.TogglePaid(sv.Id), SynchronizationBehavior.Discard, true);

            FilteredServices.Add(new ServiceRow(
                sv.Date.Day.ToString("00"),
                MonthsShort[sv.Date.Month - 1],
                dog?.Name ?? string.Empty,
                MockAppData.TypeLabel(sv.Kind),
                sv.Date.ToString("HH:mm"),
                priceLabel,
                sv.Paid,
                sv.Paid ? "Pago" : "Pendente",
                openCommand,
                toggleCommand));
        }

        HasNoServices = filtered.Count == 0;
    }

    private async Task Open(int serviceId)
    {
        mockAppData.SelectedServiceId = serviceId;
        await navigationController.PushAsync(serviceDetailFactory.Create()).WithSync();
    }
}
