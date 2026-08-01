using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

public record FutureServiceRow(string TypeLabel, string DateLabel);

[AddINotifyPropertyChangedInterface]
public class DogDetailViewModel : PresentationModelBase<Void, Void>
{
    private readonly MockAppData mockAppData;

    public DogDetailViewModel(NavigationController navigationController, MockAppData mockAppData)
    {
        this.mockAppData = mockAppData;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public string Initials { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool NoFuture { get; set; }

    public ObservableCollection<FutureServiceRow> FutureServices { get; } = [];

    protected override Task OnRunStarting(Void input)
    {
        var dog = mockAppData.DogById(mockAppData.SelectedDogId ?? -1);
        if (dog == null)
        {
            return Task.CompletedTask;
        }

        Initials = MockAppData.Initials(dog.Name);
        Name = dog.Name;
        Breed = dog.Breed;
        Description = dog.Description;
        OwnerName = mockAppData.TutorById(dog.OwnerId)?.Name ?? string.Empty;

        var now = DateTime.Now;
        var future = mockAppData.Services
            .Where(s => s.DogId == dog.Id && s.Date >= now)
            .OrderBy(s => s.Date)
            .Select(s => new FutureServiceRow(MockAppData.TypeLabel(s.Kind), s.Date.ToString("dd/MM/yyyy, HH:mm")))
            .ToList();

        FutureServices.Clear();
        foreach (var row in future)
        {
            FutureServices.Add(row);
        }

        NoFuture = future.Count == 0;

        return Task.CompletedTask;
    }
}
