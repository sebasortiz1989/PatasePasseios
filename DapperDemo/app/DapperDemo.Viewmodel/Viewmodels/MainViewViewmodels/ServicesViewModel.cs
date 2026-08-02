using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class DogOption(int id, string label)
{
    public int Id { get; } = id;

    public string Label { get; } = label;
}

[AddINotifyPropertyChangedInterface]
public class ServicesViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryDogs repositoryDogs;
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;
    private readonly EventHandler dataChangedHandler;

    public ServicesViewModel(RepositoryDogs repositoryDogs, RepositoryServices repositoryServices, AppSession session)
    {
        this.repositoryDogs = repositoryDogs;
        this.repositoryServices = repositoryServices;
        this.session = session;

        // A dog added on the Cachorros tab has to show up in this picker without a relaunch.
        dataChangedHandler = (_, _) => AppSession.FireAndForget(ReloadDogsAsync());
        session.DataChanged += dataChangedHandler;

        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);
        CreateServiceCommand = new SynchronizedCommand(CreateService, SynchronizationBehavior.Discard, true);

        SvcDatePart = DateTime.Now.Date.AddDays(1);
        SvcTimePart = TimeSpan.FromHours(9);
        SvcEndDatePart = DateTime.Now.Date.AddDays(3);
        SvcEndTimePart = TimeSpan.FromHours(18);
    }

    public ICommand SetTypeWalk { get; }

    public ICommand SetTypeSitting { get; }

    public ICommand SetTypeHotel { get; }

    public ICommand CreateServiceCommand { get; }

    public ObservableCollection<DogOption> DogOptions { get; } = [];

    /// <summary>No dogs yet means the form can't do anything useful, so it explains what to do first.</summary>
    public bool HasNoDogs { get; private set; } = true;

    public bool HasDogs => !HasNoDogs;

    public ServiceKind SvcType { get; set; } = ServiceKind.Walk;

    public bool IsTypeWalk => SvcType == ServiceKind.Walk;

    public bool IsTypeSitting => SvcType == ServiceKind.Sitting;

    public bool IsTypeHotel => SvcType == ServiceKind.Hotel;

    public bool SvcIsHotel => SvcType == ServiceKind.Hotel;

    public bool SvcIsSingleDate => SvcType != ServiceKind.Hotel;

    public DogOption? SelectedDog { get; set; }

    // Avalonia has no datetime-local control, so date and time are picked separately and recombined.
    public DateTime SvcDatePart { get; set; }

    public TimeSpan SvcTimePart { get; set; }

    public DateTime SvcEndDatePart { get; set; }

    public TimeSpan SvcEndTimePart { get; set; }

    public DateTime SvcDate => SvcDatePart.Date + SvcTimePart;

    public DateTime SvcEndDate => SvcEndDatePart.Date + SvcEndTimePart;

    public string SvcPrice { get; set; } = string.Empty;

    public string SvcPricePerDay { get; set; } = string.Empty;

    public bool SvcRequiresWalking { get; set; }

    public string SvcMsg { get; set; } = string.Empty;

    public bool HasSvcMsg => !string.IsNullOrEmpty(SvcMsg);

    public bool SvcMsgIsError { get; set; }

    protected override async Task OnRunStarting(Unit input) => await ReloadDogsAsync().WithSync();

    protected override Task OnRunFinishing()
    {
        session.DataChanged -= dataChangedHandler;
        return Task.CompletedTask;
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price);

    /// <summary>Public because the View calls it from OnLoaded — see the class remarks.</summary>
    public async Task ReloadDogsAsync()
    {
        var dogs = await repositoryDogs.ListForPetSitterAsync(session.CurrentPetSitterId).WithSync();
        var previouslySelectedId = SelectedDog?.Id;

        DogOptions.Clear();
        foreach (var dog in dogs)
        {
            DogOptions.Add(new DogOption(dog.DogId, dog.Name));
        }

        SelectedDog = previouslySelectedId is int id ? DogOptions.FirstOrDefault(o => o.Id == id) : null;
        HasNoDogs = DogOptions.Count == 0;
    }

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
                RequiresWalking = SvcRequiresWalking,
                ServicePaid = false
            }).WithSync();
        }
        else if (SvcType == ServiceKind.Sitting)
        {
            if (!TryParsePrice(SvcPrice, out var price) || price <= 0)
            {
                Fail("Informe um preço válido.");
                return;
            }

            result = await repositoryServices.AddSittingAsync(new PetSittingService
            {
                DogId = SelectedDog.Id,
                PetSitterId = session.CurrentPetSitterId,
                Date = SvcDate,
                Price = price,
                ServicePaid = false
            }).WithSync();
        }
        else
        {
            if (!TryParsePrice(SvcPrice, out var price) || price <= 0)
            {
                Fail("Informe um preço válido.");
                return;
            }

            result = await repositoryServices.AddWalkAsync(new WalkingService
            {
                DogId = SelectedDog.Id,
                PetSitterId = session.CurrentPetSitterId,
                Date = SvcDate,
                Price = price,
                ServicePaid = false
            }).WithSync();
        }

        if (result != Response.Successful)
        {
            Fail("Não foi possível agendar o serviço.");
            return;
        }

        SvcPrice = string.Empty;
        SvcPricePerDay = string.Empty;
        SvcRequiresWalking = false;
        SelectedDog = null;
        SvcMsgIsError = false;
        SvcMsg = "Serviço agendado.";

        session.NotifyDataChanged();
    }

    private void Fail(string message)
    {
        SvcMsgIsError = true;
        SvcMsg = message;
    }
}
