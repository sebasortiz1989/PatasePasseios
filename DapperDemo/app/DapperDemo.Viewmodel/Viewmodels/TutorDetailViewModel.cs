using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels;

public record TutorFutureServiceRow(string DogName, string TypeLabel, string DateLabel);

[AddINotifyPropertyChangedInterface]
public class TutorDetailViewModel : PresentationModelBase<Void, Void>
{
    private readonly MockAppData mockAppData;

    public TutorDetailViewModel(NavigationController navigationController, MockAppData mockAppData)
    {
        this.mockAppData = mockAppData;
        BackCommand = new SynchronizedCommand(() => navigationController.PopAsync(this), SynchronizationBehavior.Discard, true);
    }

    public ICommand BackCommand { get; }

    public string Initials { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string DogNames { get; set; } = string.Empty;

    public bool NoFuture { get; set; }

    public ObservableCollection<TutorFutureServiceRow> FutureServices { get; } = [];

    protected override Task OnRunStarting(Void input)
    {
        var tutor = mockAppData.TutorById(mockAppData.SelectedTutorId ?? -1);
        if (tutor == null)
        {
            return Task.CompletedTask;
        }

        Initials = MockAppData.Initials(tutor.Name);
        Name = tutor.Name;
        Neighborhood = tutor.Neighborhood;
        Phone = tutor.Phone;
        DogNames = string.Join(", ", tutor.DogIds.Select(id => mockAppData.DogById(id)?.Name).Where(n => n != null));

        var now = DateTime.Now;
        var future = mockAppData.Services
            .Where(s => tutor.DogIds.Contains(s.DogId) && s.Date >= now)
            .OrderBy(s => s.Date)
            .Select(s => new TutorFutureServiceRow(mockAppData.DogById(s.DogId)?.Name ?? string.Empty, MockAppData.TypeLabel(s.Kind), MockAppData.DateTimeLabel(s.Date)))
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
