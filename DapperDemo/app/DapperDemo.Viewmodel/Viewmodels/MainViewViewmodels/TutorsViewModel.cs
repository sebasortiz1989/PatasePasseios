using System.Collections.ObjectModel;
using System.Windows.Input;
using PropertyChanged;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;
using Verion.Threading;
using Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class TutorRow(string initials, string name, string subtitle, ICommand openCommand)
{
    public string Initials { get; } = initials;

    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    public ICommand OpenCommand { get; } = openCommand;
}

[AddINotifyPropertyChangedInterface]
public class TutorsViewModel : PresentationModelBase<Void, Void>
{
    private readonly NavigationController navigationController;
    private readonly MockAppData mockAppData;
    private readonly Factory<PresenterBase<TutorDetailViewModel, Void, Void>> tutorDetailFactory;

    public TutorsViewModel(
        NavigationController navigationController,
        MockAppData mockAppData,
        Factory<PresenterBase<TutorDetailViewModel, Void, Void>> tutorDetailFactory)
    {
        this.navigationController = navigationController;
        this.mockAppData = mockAppData;
        this.tutorDetailFactory = tutorDetailFactory;
        Refresh();
    }

    public ObservableCollection<TutorRow> TutorsCollection { get; } = [];

    public string TutorCountLabel { get; set; } = string.Empty;

    protected override Task OnRunStarting(Void input)
    {
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        TutorsCollection.Clear();
        foreach (var tutor in mockAppData.Tutors)
        {
            var dogCountLabel = tutor.DogIds.Count == 1 ? "1 cachorro" : $"{tutor.DogIds.Count} cachorros";
            // CA2000: the row owns this command for the lifetime of this view-model; the list is
            // built once from static mock data and never rebuilt, so there is nothing to release early.
#pragma warning disable CA2000
            var openCommand = new SynchronizedCommand(() => Open(tutor.Id), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000
            TutorsCollection.Add(new TutorRow(MockAppData.Initials(tutor.Name), tutor.Name, $"{tutor.Neighborhood} · {dogCountLabel}", openCommand));
        }

        TutorCountLabel = $"{mockAppData.Tutors.Count} cadastrados";
    }

    private async Task Open(int tutorId)
    {
        mockAppData.SelectedTutorId = tutorId;
        await navigationController.PushAsync(tutorDetailFactory.Create()).WithSync();
    }
}
