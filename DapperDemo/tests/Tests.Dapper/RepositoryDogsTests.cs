using PatasePasseios.Repository.Dapper;
using PatasePasseios.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// Dogs, which reach an account through their tutor rather than directly.
/// </summary>
public class RepositoryDogsTests
{
    [Fact]
    public async Task AddingADogMakesItVisibleThroughItsTutorsAccount()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        var response = await db.Dogs.Add(new Dogs
        {
            TutorId = tutorId,
            Name = "Toby",
            Breed = "Golden",
            Description = "calmo",
        });

        Assert.Equal(Response.Successful, response);

        var dogs = await db.Dogs.ListForPetSitterAsync(petSitterId);
        Assert.Single(dogs);
        Assert.Equal("Toby", dogs[0].Name);
        Assert.Equal("Golden", dogs[0].Breed);
    }

    /// <summary>Two tutors may each have a "Luna" — the name is deliberately not unique.</summary>
    [Fact]
    public async Task TwoTutorsCanEachHaveADogOfTheSameName()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var first = await db.SeedTutorAsync(petSitterId, "Ana");
        var second = await db.SeedTutorAsync(petSitterId, "Marina");

        await db.SeedDogAsync(first, "Luna");
        await db.SeedDogAsync(second, "Luna");

        Assert.Equal(2, (await db.Dogs.ListForPetSitterAsync(petSitterId)).Length);
        Assert.Single(await db.Dogs.ListForTutorAsync(first));
        Assert.Single(await db.Dogs.ListForTutorAsync(second));
    }

    [Fact]
    public async Task ADogIsInvisibleToAnotherAccount()
    {
        using var db = new TestDatabase();
        var (mine, _, _) = await db.SeedAccountAsync();

        await db.PetSitters.Add(new PetSitter
        {
            Email = "outra@test.com",
            PasswordHash = string.Empty,
            Password = "senha",
            Name = "Outra",
            BirthDate = DateTime.Now,
        });

        var theirs = (await db.PetSitters.GetByEmailAsync("outra@test.com"))!.PetSitterId;

        Assert.Single(await db.Dogs.ListForPetSitterAsync(mine));
        Assert.Empty(await db.Dogs.ListForPetSitterAsync(theirs));
    }

    [Fact]
    public async Task UpdateSavesTheEditedFields()
    {
        using var db = new TestDatabase();
        var (_, tutorId, dogId) = await db.SeedAccountAsync();

        var response = await db.Dogs.Update(new Dogs
        {
            DogId = dogId,
            TutorId = tutorId,
            Name = "Toby II",
            Breed = "Labrador",
            Description = "brincalhão",
            Image = "foto.jpg",
        });

        Assert.Equal(Response.Successful, response);

        var dog = await db.Dogs.GetAsync(dogId);
        Assert.Equal("Toby II", dog!.Name);
        Assert.Equal("Labrador", dog.Breed);
        Assert.Equal("brincalhão", dog.Description);
        Assert.Equal("foto.jpg", dog.Image);
    }

    /// <summary>Clearing the photo is an ordinary update, not a special case.</summary>
    [Fact]
    public async Task UpdateCanClearThePhoto()
    {
        using var db = new TestDatabase();
        var (_, tutorId, dogId) = await db.SeedAccountAsync();

        await db.Dogs.Update(new Dogs { DogId = dogId, TutorId = tutorId, Name = "Toby", Image = "foto.jpg" });
        Assert.Equal("foto.jpg", (await db.Dogs.GetAsync(dogId))!.Image);

        await db.Dogs.Update(new Dogs { DogId = dogId, TutorId = tutorId, Name = "Toby", Image = null });
        Assert.Null((await db.Dogs.GetAsync(dogId))!.Image);
    }

    [Fact]
    public async Task UpdateCanMoveADogToAnotherTutor()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var from = await db.SeedTutorAsync(petSitterId, "Ana");
        var to = await db.SeedTutorAsync(petSitterId, "Marina");
        var dogId = await db.SeedDogAsync(from, "Toby");

        await db.Dogs.Update(new Dogs { DogId = dogId, TutorId = to, Name = "Toby" });

        Assert.Empty(await db.Dogs.ListForTutorAsync(from));
        Assert.Single(await db.Dogs.ListForTutorAsync(to));
    }

    /// <summary>
    /// The service tables carry no ON DELETE CASCADE, so removing a dog has to remove its
    /// bookings too or the agenda joins against rows whose dog no longer exists.
    /// </summary>
    [Fact]
    public async Task DeletingADogTakesItsServicesWithIt()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = new DateTime(2026, 8, 1, 9, 0, 0),
            Price = 40m,
        });
        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = new DateTime(2026, 8, 2, 9, 0, 0),
            EndDate = new DateTime(2026, 8, 5, 9, 0, 0),
            PricePerDay = 100m,
        });

        Assert.Equal(2, (await db.Services.ListForPetSitterAsync(petSitterId)).Length);

        var response = await db.Dogs.Delete(dogId);

        Assert.Equal(Response.Successful, response);
        Assert.Empty(await db.Dogs.ListForPetSitterAsync(petSitterId));
        Assert.Empty(await db.Services.ListForPetSitterAsync(petSitterId));
    }

    /// <summary>Deleting one dog must leave its tutor and their other dogs standing.</summary>
    [Fact]
    public async Task DeletingADogLeavesTheTutorAndSiblingsAlone()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);
        var doomed = await db.SeedDogAsync(tutorId, "Toby");
        await db.SeedDogAsync(tutorId, "Luna");

        await db.Dogs.Delete(doomed);

        Assert.Single(await db.Tutors.ListForPetSitterAsync(petSitterId));
        var remaining = await db.Dogs.ListForTutorAsync(tutorId);
        Assert.Single(remaining);
        Assert.Equal("Luna", remaining[0].Name);
    }

    [Fact]
    public async Task GettingADogThatDoesNotExistReturnsNull()
    {
        using var db = new TestDatabase();

        Assert.Null(await db.Dogs.GetAsync(999));
    }
}