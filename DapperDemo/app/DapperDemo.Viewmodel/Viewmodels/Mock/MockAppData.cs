namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

/// <summary>
/// Shared in-memory data for every screen after login, standing in for the real
/// Tutors/Services backend until that work resumes. Registered as a DI singleton so
/// every tab/detail ViewModel sees the same state; also carries the "selected id" for
/// detail screens, since Factory&lt;PresenterBase&lt;T,Void,Void&gt;&gt;.Create() takes no runtime args.
/// </summary>
public class MockAppData
{
    private int nextServiceId;

    public MockAppData()
    {
        Tutors =
        [
            new MockTutor { Id = 1, Name = "Marina Alves", Phone = "(11) 98221-4432", Neighborhood = "Vila Madalena", DogIds = [1, 2] },
            new MockTutor { Id = 2, Name = "Rafael Souza", Phone = "(11) 97733-1290", Neighborhood = "Pinheiros", DogIds = [3] },
            new MockTutor { Id = 3, Name = "Beatriz Lima", Phone = "(11) 96654-8871", Neighborhood = "Itaim Bibi", DogIds = [4] },
            new MockTutor { Id = 4, Name = "João Pedro", Phone = "(11) 99887-6633", Neighborhood = "Moema", DogIds = [5] }
        ];

        Dogs =
        [
            new MockDog { Id = 1, OwnerId = 1, Name = "Toby", Breed = "Golden Retriever", Description = "Muito brincalhão, adora bola e convive bem com outros cachorros. Puxa a guia no começo do passeio." },
            new MockDog { Id = 2, OwnerId = 1, Name = "Nina", Breed = "Vira-lata", Description = "Adora correr solta no parque; um pouco medrosa com barulhos altos e motos." },
            new MockDog { Id = 3, OwnerId = 2, Name = "Bento", Breed = "Beagle", Description = "Late bastante quando fica sozinho e precisa de bastante exercício antes de descansar." },
            new MockDog { Id = 4, OwnerId = 3, Name = "Mel", Breed = "Poodle", Description = "Calma e carinhosa. Precisa de escovação depois do banho e não gosta de chuva." },
            new MockDog { Id = 5, OwnerId = 4, Name = "Zeca", Breed = "Labrador", Description = "Adora água e tem ótimo apetite. Toma remédio de manhã, sempre com comida." }
        ];

        Services =
        [
            new MockService { Id = 1, Kind = ServiceKind.Walk, DogId = 1, Date = new DateTime(2026, 8, 1, 8, 0, 0), Price = 40, Paid = false },
            new MockService { Id = 2, Kind = ServiceKind.Sitting, DogId = 2, Date = new DateTime(2026, 8, 2, 14, 0, 0), Price = 80, Paid = true },
            new MockService { Id = 3, Kind = ServiceKind.Hotel, DogId = 3, Date = new DateTime(2026, 8, 3, 9, 0, 0), EndDate = new DateTime(2026, 8, 6, 18, 0, 0), PricePerDay = 60, RequiresWalking = true, Paid = false },
            new MockService { Id = 4, Kind = ServiceKind.Walk, DogId = 4, Date = new DateTime(2026, 8, 3, 10, 0, 0), Price = 45, Paid = false },
            new MockService { Id = 5, Kind = ServiceKind.Sitting, DogId = 5, Date = new DateTime(2026, 7, 25, 9, 0, 0), Price = 90, Paid = true },
            new MockService { Id = 6, Kind = ServiceKind.Walk, DogId = 1, Date = new DateTime(2026, 7, 31, 8, 0, 0), Price = 40, Paid = true },
            new MockService { Id = 7, Kind = ServiceKind.Hotel, DogId = 2, Date = new DateTime(2026, 8, 8, 9, 0, 0), EndDate = new DateTime(2026, 8, 10, 9, 0, 0), PricePerDay = 55, RequiresWalking = false, Paid = false },
            new MockService { Id = 8, Kind = ServiceKind.Walk, DogId = 3, Date = new DateTime(2026, 8, 1, 16, 0, 0), Price = 35, Paid = true }
        ];

        nextServiceId = Services.Max(s => s.Id) + 1;
    }

    public List<MockTutor> Tutors { get; }

    public List<MockDog> Dogs { get; }

    public List<MockService> Services { get; }

    public string CurrentUserName { get; set; } = "Ana Ferreira";

    public int? SelectedDogId { get; set; }

    public int? SelectedTutorId { get; set; }

    public int? SelectedServiceId { get; set; }

    public event Action? DataChanged;

    public event Action? LogoutRequested;

    public MockDog? DogById(int id) => Dogs.FirstOrDefault(d => d.Id == id);

    public MockTutor? TutorById(int id) => Tutors.FirstOrDefault(t => t.Id == id);

    public MockTutor? TutorOfDog(int dogId)
    {
        var dog = DogById(dogId);
        return dog == null ? null : TutorById(dog.OwnerId);
    }

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0])));

    public static string TypeLabel(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => "Passeio",
        ServiceKind.Sitting => "Pet sitting",
        ServiceKind.Hotel => "Hotel",
        _ => string.Empty
    };

    public static string Money(decimal value) => "R$ " + value.ToString("F2").Replace('.', ',');

    public void TogglePaid(int serviceId)
    {
        var service = Services.FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
        {
            return;
        }

        service.Paid = !service.Paid;
        DataChanged?.Invoke();
    }

    public void AddService(MockService service)
    {
        Services.Add(service with { Id = nextServiceId++ });
        DataChanged?.Invoke();
    }

    public void ChangePassword(string newPassword)
    {
        // Front-end only for now: nothing to persist against yet, this just completes the round-trip.
        DataChanged?.Invoke();
    }

    public void SetCurrentUserName(string name)
    {
        CurrentUserName = name;
        DataChanged?.Invoke();
    }

    public void RequestLogout() => LogoutRequested?.Invoke();
}
