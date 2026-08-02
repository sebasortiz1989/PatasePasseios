using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class DogOption(int id, string label)
{
    public int Id { get; } = id;

    public string Label { get; } = label;
}

[AddINotifyPropertyChangedInterface]
public class ServicesViewModel : PresentationModelBase<Void, Void>
{
    private readonly MockAppData mockAppData;

    public ServicesViewModel(MockAppData mockAppData)
    {
        this.mockAppData = mockAppData;

        SetTypeWalk = new SynchronizedCommand(() => SetType(ServiceKind.Walk), SynchronizationBehavior.Discard, true);
        SetTypeSitting = new SynchronizedCommand(() => SetType(ServiceKind.Sitting), SynchronizationBehavior.Discard, true);
        SetTypeHotel = new SynchronizedCommand(() => SetType(ServiceKind.Hotel), SynchronizationBehavior.Discard, true);
        CreateServiceCommand = new SynchronizedCommand(CreateService, SynchronizationBehavior.Discard, true);

        foreach (var dog in mockAppData.Dogs)
        {
            DogOptions.Add(new DogOption(dog.Id, dog.Name));
        }

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

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private void SetType(ServiceKind kind)
    {
        SvcType = kind;
        SvcMsg = string.Empty;
    }

    private void CreateService()
    {
        if (SelectedDog == null)
        {
            Fail("Selecione um cachorro.");
            return;
        }

        if (SvcType == ServiceKind.Hotel)
        {
            if (!decimal.TryParse(SvcPricePerDay, out var pricePerDay) || pricePerDay <= 0)
            {
                Fail("Preencha as datas e o preço por dia.");
                return;
            }

            if (SvcEndDate <= SvcDate)
            {
                Fail("A saída deve ser depois da entrada.");
                return;
            }

            mockAppData.AddService(new MockService
            {
                Kind = ServiceKind.Hotel,
                DogId = SelectedDog.Id,
                Date = SvcDate,
                EndDate = SvcEndDate,
                PricePerDay = pricePerDay,
                RequiresWalking = SvcRequiresWalking,
                Paid = false
            });

            SvcPricePerDay = string.Empty;
            SvcRequiresWalking = false;
        }
        else
        {
            if (!decimal.TryParse(SvcPrice, out var price) || price <= 0)
            {
                Fail("Preencha a data e o preço.");
                return;
            }

            mockAppData.AddService(new MockService
            {
                Kind = SvcType,
                DogId = SelectedDog.Id,
                Date = SvcDate,
                Price = price,
                Paid = false
            });

            SvcPrice = string.Empty;
        }

        SelectedDog = null;
        SvcMsgIsError = false;
        SvcMsg = "Serviço agendado.";
    }

    private void Fail(string message)
    {
        SvcMsgIsError = true;
        SvcMsg = message;
    }
}
