using System.Windows.Input;
using PropertyChanged;
using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class ServiceDetailViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly RepositoryServices repositoryServices;
    private readonly AppSession session;

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
    }

    public ICommand BackCommand { get; }

    public ICommand TogglePaidCommand { get; }

    public ICommand AskDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>Deleting takes two taps: the button swaps for a confirm/cancel pair.</summary>
    public bool ConfirmingDelete { get; private set; }

    public bool NotConfirmingDelete => !ConfirmingDelete;

    public string TypeLabel { get; private set; } = string.Empty;

    public string DogName { get; private set; } = string.Empty;

    public string TutorName { get; private set; } = string.Empty;

    public string DateLabel { get; private set; } = string.Empty;

    public bool IsHotel { get; private set; }

    public string EndLabel { get; private set; } = string.Empty;

    public string WalkingLabel { get; private set; } = string.Empty;

    public string PriceFieldLabel { get; private set; } = string.Empty;

    public string PriceLabel { get; private set; } = string.Empty;

    public bool Paid { get; private set; }

    public string PaidActionLabel { get; private set; } = string.Empty;

    protected override async Task OnRunStarting(Unit input) => await LoadAsync().WithSync();

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

        TypeLabel = AppSession.TypeLabel(service.Kind);
        DogName = service.DogName;
        TutorName = service.TutorName;
        DateLabel = AppSession.DateTimeLabel(service.Date);
        IsHotel = service.Kind == ServiceKind.Hotel;
        EndLabel = service.EndDate is DateTime end ? AppSession.DateTimeLabel(end) : string.Empty;
        WalkingLabel = service.RequiresWalking ? "Incluídos" : "Não incluídos";
        PriceFieldLabel = service.Kind == ServiceKind.Hotel ? "Preço por dia" : "Preço";
        PriceLabel = service.Kind == ServiceKind.Hotel
            ? AppSession.Money(service.Price) + " / dia"
            : AppSession.Money(service.Price);
        Paid = service.ServicePaid;
        PaidActionLabel = service.ServicePaid ? "Pago — marcar pendente" : "Marcar como pago";
    }
}
