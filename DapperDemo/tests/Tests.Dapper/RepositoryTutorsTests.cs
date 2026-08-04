using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// Tutors, and the account scoping that keeps one pet sitter's records out of another's.
/// </summary>
public class RepositoryTutorsTests
{
    [Fact]
    public async Task AddingATutorLinksItToTheAccountThatCreatedIt()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();

        var response = await db.Tutors.AddForPetSitterAsync(
            new Tutors { Name = "Ana", Telephone = "99999", Address = "Centro" },
            petSitterId);

        Assert.Equal(Response.Successful, response);

        var tutors = await db.Tutors.ListForPetSitterAsync(petSitterId);
        Assert.Single(tutors);
        Assert.Equal("Ana", tutors[0].Name);
    }

    /// <summary>
    /// The whole point of the join table: a tutor belongs to the account that created them, and
    /// another account must not see them.
    /// </summary>
    [Fact]
    public async Task ATutorIsInvisibleToAnotherAccount()
    {
        using var db = new TestDatabase();
        var mine = await db.SeedPetSitterAsync();
        await db.SeedTutorAsync(mine, "Ana");

        await db.PetSitters.Add(new PetSitter
        {
            Email = "outra@test.com",
            PasswordHash = string.Empty,
            Password = "senha",
            Name = "Outra",
            BirthDate = DateTime.Now,
        });

        var theirs = (await db.PetSitters.GetByEmailAsync("outra@test.com"))!.PetSitterId;

        Assert.Single(await db.Tutors.ListForPetSitterAsync(mine));
        Assert.Empty(await db.Tutors.ListForPetSitterAsync(theirs));
    }

    [Fact]
    public async Task TutorsComeBackAlphabetically()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        await db.SeedTutorAsync(petSitterId, "Zoe");
        await db.SeedTutorAsync(petSitterId, "Ana");
        await db.SeedTutorAsync(petSitterId, "Marina");

        var names = (await db.Tutors.ListForPetSitterAsync(petSitterId)).Select(t => t.Name).ToArray();

        Assert.Equal(new[] { "Ana", "Marina", "Zoe" }, names);
    }

    [Fact]
    public async Task UpdateSavesTheEditedFields()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        var response = await db.Tutors.Update(new Tutors
        {
            TutorId = tutorId,
            Name = "Ana Paula",
            Telephone = "88888",
            Address = "Endereço",
        });

        Assert.Equal(Response.Successful, response);

        var tutor = await db.Tutors.GetAsync(tutorId);
        Assert.Equal("Ana Paula", tutor!.Name);
        Assert.Equal("88888", tutor.Telephone);
        Assert.Equal("Endereço", tutor.Address);
    }

    /// <summary>An edit must not detach the tutor from the account that owns them.</summary>
    [Fact]
    public async Task UpdateKeepsTheTutorOnTheSameAccount()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        await db.Tutors.Update(new Tutors { TutorId = tutorId, Name = "Renomeada", Telephone = "1", Address = "x" });

        Assert.Single(await db.Tutors.ListForPetSitterAsync(petSitterId));
    }

    /// <summary>
    /// Nothing cascades in the schema, so the delete has to sweep up the tutor's dogs and their
    /// services itself — otherwise the agenda joins against rows whose dog is gone.
    /// </summary>
    [Fact]
    public async Task DeletingATutorTakesTheirDogsAndServicesWithThem()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = new DateTime(2026, 8, 1, 9, 0, 0),
            Price = 40m,
        });

        Assert.Single(await db.Services.ListForPetSitterAsync(petSitterId));

        var response = await db.Tutors.Delete(tutorId);

        Assert.Equal(Response.Successful, response);
        Assert.Empty(await db.Tutors.ListForPetSitterAsync(petSitterId));
        Assert.Empty(await db.Dogs.ListForPetSitterAsync(petSitterId));
        Assert.Empty(await db.Services.ListForPetSitterAsync(petSitterId));
    }

    /// <summary>Deleting one tutor must not disturb another's records.</summary>
    [Fact]
    public async Task DeletingOneTutorLeavesTheOthersAlone()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var doomed = await db.SeedTutorAsync(petSitterId, "Ana");
        var keeper = await db.SeedTutorAsync(petSitterId, "Marina");
        await db.SeedDogAsync(keeper, "Luna");

        await db.Tutors.Delete(doomed);

        var remaining = await db.Tutors.ListForPetSitterAsync(petSitterId);
        Assert.Single(remaining);
        Assert.Equal("Marina", remaining[0].Name);
        Assert.Single(await db.Dogs.ListForTutorAsync(keeper));
    }

    [Fact]
    public async Task GettingATutorThatDoesNotExistReturnsNull()
    {
        using var db = new TestDatabase();

        Assert.Null(await db.Tutors.GetAsync(999));
    }

    [Fact]
    public async Task ATutorStartsWithNoCredit()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    [Fact]
    public async Task CreditSurvivesAReRead()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        Assert.Equal(Response.Successful, await db.Tutors.SetCreditAsync(tutorId, 125.50m));

        Assert.Equal(125.50m, (await db.Tutors.GetAsync(tutorId))!.Credit);
        Assert.Equal(125.50m, (await db.Tutors.ListForPetSitterAsync(petSitterId)).Single().Credit);
    }

    /// <summary>Spending more credit than exists must not leave a negative balance behind.</summary>
    [Fact]
    public async Task CreditNeverGoesNegative()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);

        await db.Tutors.SetCreditAsync(tutorId, -40m);

        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    /// <summary>Editing a phone number must not spend the tutor's credit.</summary>
    [Fact]
    public async Task EditingATutorLeavesTheirCreditAlone()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);
        await db.Tutors.SetCreditAsync(tutorId, 80m);

        await db.Tutors.Update(new Tutors { TutorId = tutorId, Name = "Ana Paula", Telephone = "1", Address = "x" });

        Assert.Equal(80m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    /// <summary>
    /// The unscoped entry points are deliberately unavailable — a tutor without an account link
    /// would be invisible to everyone.
    /// </summary>
    [Fact]
    public async Task TheUnscopedAddIsRefused()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => db.Tutors.Add(new Tutors { Name = "Solta", Telephone = "1", Address = "x" }));
    }
}