using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

[AddINotifyPropertyChangedInterface]
public class ServiceDetailViewModel : PresentationModelBase<Void, Void>
{
    private readonly MockAppData mockAppData;

    public ServiceDetailViewModel(NavigationController navigationController, MockAppData mockAppData)
    {
        this.mockAppData = mockAppData;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
        TogglePaidCommand = new SynchronizedCommand(TogglePaid, SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public ICommand TogglePaidCommand { get; }

    public string TypeLabel { get; set; } = string.Empty;

    public string DogName { get; set; } = string.Empty;

    public string TutorName { get; set; } = string.Empty;

    public string DateLabel { get; set; } = string.Empty;

    public bool IsHotel { get; set; }

    public string EndLabel { get; set; } = string.Empty;

    public string WalkingLabel { get; set; } = string.Empty;

    public string PriceFieldLabel { get; set; } = string.Empty;

    public string PriceLabel { get; set; } = string.Empty;

    public bool Paid { get; set; }

    public string PaidActionLabel { get; set; } = string.Empty;

    protected override Task OnRunStarting(Void input)
    {
        Load();
        return Task.CompletedTask;
    }

    private void TogglePaid()
    {
        if (mockAppData.SelectedServiceId is int id)
        {
            mockAppData.TogglePaid(id);
        }

        Load();
    }

    private void Load()
    {
        var service = mockAppData.Services.FirstOrDefault(s => s.Id == mockAppData.SelectedServiceId);
        if (service == null)
        {
            return;
        }

        var dog = mockAppData.DogById(service.DogId);
        var tutor = mockAppData.TutorOfDog(service.DogId);

        TypeLabel = MockAppData.TypeLabel(service.Kind);
        DogName = dog?.Name ?? string.Empty;
        TutorName = tutor?.Name ?? string.Empty;
        DateLabel = MockAppData.DateTimeLabel(service.Date);
        IsHotel = service.Kind == ServiceKind.Hotel;
        EndLabel = service.EndDate is DateTime end ? MockAppData.DateTimeLabel(end) : string.Empty;
        WalkingLabel = service.RequiresWalking ? "Incluídos" : "Não incluídos";
        PriceFieldLabel = service.Kind == ServiceKind.Hotel ? "Preço por dia" : "Preço";
        PriceLabel = service.Kind == ServiceKind.Hotel
            ? MockAppData.Money(service.PricePerDay ?? 0) + " / dia"
            : MockAppData.Money(service.Price ?? 0);
        Paid = service.Paid;
        PaidActionLabel = service.Paid ? "Pago — marcar pendente" : "Marcar como pago";
    }
}
