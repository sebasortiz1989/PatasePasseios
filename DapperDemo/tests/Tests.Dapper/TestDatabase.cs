using PatasePasseios.Repository.Dapper;
using PatasePasseios.Repository.Dapper.Aggregates;
using PatasePasseios.Repository.Dapper.Dtos;
using PatasePasseios.Repository.Dapper.Services;

namespace Tests.Dapper;

/// <summary>
/// A real database, built by the real schema, thrown away when the test finishes.
/// </summary>
/// <remarks>
/// The repositories are exercised against SQLite rather than a mock on purpose: almost everything
/// they do is SQL, so a mocked connection would only prove the tests agree with themselves. A
/// file per test rather than an in-memory database, because the repositories open a fresh
/// connection per call and an in-memory database dies with the first one that closes.
/// </remarks>
public sealed class TestDatabase : IDisposable
{
    private readonly string path;

    public TestDatabase()
    {
        path = Path.Combine(Path.GetTempPath(), $"patasepasseios-tests-{Guid.NewGuid():N}.db");
        Database = new DapperDatabaseService(path);

        PetSitters = new RepositoryPetSitter(Database);
        Tutors = new RepositoryTutors(Database);
        Dogs = new RepositoryDogs(Database);
        Services = new RepositoryServices(Database);
        Payments = new RepositoryPayments(Database, Services);
    }

    public DapperDatabaseService Database { get; }

    public RepositoryPetSitter PetSitters { get; }

    public RepositoryTutors Tutors { get; }

    public RepositoryDogs Dogs { get; }

    public RepositoryServices Services { get; }

    /// <summary>The ledger of money received, which is also what unwinds a payment.</summary>
    public RepositoryPayments Payments { get; }

    /// <summary>
    /// The account the schema seeds itself with. Every scoped read hangs off a pet sitter, so
    /// tests need one before they can do anything.
    /// </summary>
    public async Task<int> SeedPetSitterAsync()
    {
        var petSitter = await PetSitters.GetByEmailAsync("test@test.com");
        return petSitter!.PetSitterId;
    }

    /// <summary>Adds a tutor to the given account and returns its id.</summary>
    public async Task<int> SeedTutorAsync(int petSitterId, string name = "Ana")
    {
        await Tutors.AddForPetSitterAsync(
            new Tutors { Name = name, Telephone = "99999", Address = "Centro" },
            petSitterId);

        var tutors = await Tutors.ListForPetSitterAsync(petSitterId);
        return tutors.Single(t => t.Name == name).TutorId;
    }

    /// <summary>Adds a dog under the given tutor and returns its id.</summary>
    public async Task<int> SeedDogAsync(int tutorId, string name = "Toby")
    {
        await Dogs.Add(new Dogs { TutorId = tutorId, Name = name, Breed = "Lab", Description = "calmo" });

        var dogs = await Dogs.ListForTutorAsync(tutorId);
        return dogs.Single(d => d.Name == name).DogId;
    }

    /// <summary>An account with one tutor and one dog — the starting point for most tests.</summary>
    public async Task<(int PetSitterId, int TutorId, int DogId)> SeedAccountAsync()
    {
        var petSitterId = await SeedPetSitterAsync();
        var tutorId = await SeedTutorAsync(petSitterId);
        var dogId = await SeedDogAsync(tutorId);
        return (petSitterId, tutorId, dogId);
    }

    public void Dispose()
    {
        // The connection pool holds the file open on Windows, so it has to be emptied before the
        // delete can succeed.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // A restore leaves the database it replaced beside the live one; a test that drove
            // one should not leave that copy behind in the system temp folder.
            if (File.Exists(path + ".replaced"))
            {
                File.Delete(path + ".replaced");
            }
        }
        catch (IOException)
        {
            // A leaked handle should not fail an otherwise passing test; the file is in the
            // system temp folder and will be cleaned up there.
        }
    }
}