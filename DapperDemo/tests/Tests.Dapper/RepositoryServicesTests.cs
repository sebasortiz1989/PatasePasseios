using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The agenda: four tables read as one list, edited, settled and billed.
/// </summary>
public class RepositoryServicesTests
{
    private static readonly DateTime August1 = new(2026, 8, 1, 9, 0, 0);

    [Fact]
    public async Task AllFourKindsComeBackFromTheOneAgendaRead()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await AddOneOfEachAsync(db, petSitterId, dogId);

        var services = await db.Services.ListForPetSitterAsync(petSitterId);

        Assert.Equal(4, services.Length);
        Assert.Contains(services, s => s.Kind == ServiceKind.Walk);
        Assert.Contains(services, s => s.Kind == ServiceKind.Sitting);
        Assert.Contains(services, s => s.Kind == ServiceKind.Hotel);
        Assert.Contains(services, s => s.Kind == ServiceKind.DayCare);
    }

    [Fact]
    public async Task TheAgendaIsOrderedByDateAcrossKinds()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await AddOneOfEachAsync(db, petSitterId, dogId);

        var dates = (await db.Services.ListForPetSitterAsync(petSitterId)).Select(s => s.Date).ToArray();

        Assert.Equal(dates.OrderBy(d => d), dates);
    }

    /// <summary>
    /// Each table is queried on its own rather than in a UNION so SQLite keeps every column's
    /// declared type — the hotel check-out used to come back as an unmappable string.
    /// </summary>
    [Fact]
    public async Task AHotelStayKeepsItsCheckOutDateAsADate()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(3),
            PricePerDay = 100m,
            RequiresWalking = true,
        });

        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal(August1, stay.Date);
        Assert.Equal(August1.AddDays(3), stay.EndDate);
        Assert.True(stay.RequiresWalking);
        Assert.Equal(3, stay.Nights);
        Assert.Equal(300m, stay.Total);
    }

    /// <summary>Only a hotel stay has a check-out; the others must come back without one.</summary>
    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.DayCare)]
    public async Task NonHotelKindsHaveNoCheckOutDate(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await AddAsync(db, kind, petSitterId, dogId, August1, 100m);

        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Null(service.EndDate);
        Assert.Equal(100m, service.Total);
    }

    [Fact]
    public async Task TheAgendaCarriesTheDogAndTutorItJoinsTo()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId, "Ana");
        var dogId = await db.SeedDogAsync(tutorId, "Toby");
        await db.Dogs.Update(new Dogs { DogId = dogId, TutorId = tutorId, Name = "Toby", Image = "toby.jpg" });

        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 40m);

        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal("Toby", service.DogName);
        Assert.Equal("Ana", service.TutorName);
        Assert.Equal("Centro", service.TutorAddress);
        Assert.Equal("toby.jpg", service.DogImage);
    }

    [Fact]
    public async Task ListForDogOnlyReturnsThatDogsServices()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId);
        var toby = await db.SeedDogAsync(tutorId, "Toby");
        var luna = await db.SeedDogAsync(tutorId, "Luna");

        await AddAsync(db, ServiceKind.Walk, petSitterId, toby, August1, 40m);
        await AddAsync(db, ServiceKind.Sitting, petSitterId, luna, August1.AddDays(1), 80m);

        var tobys = await db.Services.ListForDogAsync(petSitterId, toby);

        Assert.Single(tobys);
        Assert.Equal("Toby", tobys[0].DogName);
    }

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public async Task AServiceCanBeMarkedPaidAndPendingAgain(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, kind, petSitterId, dogId, August1, 100m);
        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.False(service.ServicePaid);

        await db.Services.SetPaidAsync(kind, service.ServiceId, true);
        Assert.True((await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!.ServicePaid);

        await db.Services.SetPaidAsync(kind, service.ServiceId, false);
        Assert.False((await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!.ServicePaid);
    }

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public async Task AServiceCanBeMarkedDoneAndNotDoneAgain(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, kind, petSitterId, dogId, August1, 100m);
        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.False(service.ServiceDone);

        await db.Services.SetDoneAsync(kind, service.ServiceId, true);
        Assert.True((await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!.ServiceDone);

        await db.Services.SetDoneAsync(kind, service.ServiceId, false);
        Assert.False((await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!.ServiceDone);
    }

    /// <summary>
    /// The two flags answer different questions, so neither write may disturb the other: work is
    /// often done before it is settled, and occasionally settled before it is done.
    /// </summary>
    [Fact]
    public async Task MarkingAServiceDoneLeavesItsPaymentAlone()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 100m);
        var walk = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.SetDoneAsync(ServiceKind.Walk, walk.ServiceId, true);

        var done = (await db.Services.GetAsync(petSitterId, ServiceKind.Walk, walk.ServiceId))!;
        Assert.True(done.ServiceDone);
        Assert.False(done.ServicePaid);
        Assert.Equal(100m, done.AmountDue);

        await db.Services.SetPaidAsync(ServiceKind.Walk, walk.ServiceId, true);

        var settled = (await db.Services.GetAsync(petSitterId, ServiceKind.Walk, walk.ServiceId))!;
        Assert.True(settled.ServiceDone);
        Assert.True(settled.ServicePaid);
    }

    /// <summary>A booking can be paid up front and still not have happened yet.</summary>
    [Fact]
    public async Task SettlingAServiceDoesNotMarkItDone()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Sitting, petSitterId, dogId, August1, 80m);
        var sitting = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.RegisterPaymentAsync([new ServicePayment(sitting.Kind, sitting.ServiceId, sitting.Price, true)]);

        var after = (await db.Services.GetAsync(petSitterId, ServiceKind.Sitting, sitting.ServiceId))!;
        Assert.True(after.ServicePaid);
        Assert.False(after.ServiceDone);
    }

    /// <summary>An edit changes the date and the price; it must not silently un-tick the work.</summary>
    [Fact]
    public async Task EditingAServiceKeepsItsDoneFlag()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 100m);
        var walk = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        await db.Services.SetDoneAsync(ServiceKind.Walk, walk.ServiceId, true);

        await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = walk.ServiceId,
            Kind = ServiceKind.Walk,
            DogId = dogId,
            DogName = walk.DogName,
            TutorName = walk.TutorName,
            Date = August1.AddDays(1),
            Price = 120m,
        });

        var edited = (await db.Services.GetAsync(petSitterId, ServiceKind.Walk, walk.ServiceId))!;
        Assert.Equal(120m, edited.Price);
        Assert.True(edited.ServiceDone);
    }

    /// <summary>A booking that carries the flag at insert time keeps it — the column is really written.</summary>
    [Fact]
    public async Task AServiceCanBeCreatedAlreadyDone()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = August1,
            Price = 40m,
            ServiceDone = true,
        });

        var walk = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.True(walk.ServiceDone);
        Assert.False(walk.ServicePaid);
    }

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    public async Task EditingAServiceSavesItsDateAndPrice(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, kind, petSitterId, dogId, August1, 100m);
        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        var response = await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = service.ServiceId,
            Kind = kind,
            DogId = dogId,
            DogName = service.DogName,
            TutorName = service.TutorName,
            Date = August1.AddDays(5).AddHours(3),
            Price = 55m,
        });

        Assert.Equal(Response.Successful, response);

        var edited = (await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!;
        Assert.Equal(August1.AddDays(5).AddHours(3), edited.Date);
        Assert.Equal(55m, edited.Price);
    }

    /// <summary>
    /// Day-care covers a whole day and has no time of day, so an edit stores the date at midnight
    /// however the caller happens to have filled in the clock.
    /// </summary>
    [Fact]
    public async Task EditingDayCareKeepsTheDateAndDropsTheTime()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.DayCare, petSitterId, dogId, August1, 100m);
        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = service.ServiceId,
            Kind = ServiceKind.DayCare,
            DogId = dogId,
            DogName = service.DogName,
            TutorName = service.TutorName,
            Date = August1.AddDays(5).AddHours(3),
            Price = 55m,
        });

        var edited = (await db.Services.GetAsync(petSitterId, ServiceKind.DayCare, service.ServiceId))!;
        Assert.Equal(August1.AddDays(5).Date, edited.Date);
        Assert.Equal(TimeSpan.Zero, edited.Date.TimeOfDay);
        Assert.Equal(55m, edited.Price);
    }

    /// <summary>A hotel edit also carries the check-out and whether walks are included.</summary>
    [Fact]
    public async Task EditingAHotelStaySavesItsCheckOutAndWalks()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Hotel, petSitterId, dogId, August1, 100m);
        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = stay.ServiceId,
            Kind = ServiceKind.Hotel,
            DogId = dogId,
            DogName = stay.DogName,
            TutorName = stay.TutorName,
            Date = August1,
            EndDate = August1.AddDays(5),
            Price = 90m,
            RequiresWalking = true,
        });

        var edited = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.Equal(August1.AddDays(5), edited.EndDate);
        Assert.Equal(90m, edited.Price);
        Assert.True(edited.RequiresWalking);
        Assert.Equal(450m, edited.Total);
    }

    [Fact]
    public async Task DeletingAServiceLeavesTheOthersStanding()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddOneOfEachAsync(db, petSitterId, dogId);
        var walk = (await db.Services.ListForPetSitterAsync(petSitterId)).Single(s => s.Kind == ServiceKind.Walk);

        var response = await db.Services.DeleteAsync(ServiceKind.Walk, walk.ServiceId);

        Assert.Equal(Response.Successful, response);
        var remaining = await db.Services.ListForPetSitterAsync(petSitterId);
        Assert.Equal(3, remaining.Length);
        Assert.DoesNotContain(remaining, s => s.Kind == ServiceKind.Walk);
    }

    /// <summary>
    /// A payment settles the services it covers in full. The part-covered one keeps its unpaid
    /// flag and its price, recording what was settled against it — the balance lives in
    /// <see cref="ServiceItem.Outstanding"/>, not in a cut-down price.
    /// </summary>
    [Fact]
    public async Task RegisteringAPaymentSettlesSomeServicesAndPartSettlesOne()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 100m);
        await AddAsync(db, ServiceKind.Sitting, petSitterId, dogId, August1.AddDays(1), 100m);

        var booked = await db.Services.ListForPetSitterAsync(petSitterId);
        foreach (var service in booked)
        {
            // Both carried out, so both are chargeable — a payment can only land on executed work.
            await db.Services.SetDoneAsync(service.Kind, service.ServiceId, true);
        }

        var before = await db.Services.ListForPetSitterAsync(petSitterId);
        var first = before.Single(s => s.Kind == ServiceKind.Walk);
        var second = before.Single(s => s.Kind == ServiceKind.Sitting);

        // 175 received: the first service is cleared, and 75 of the second — leaving 25 owing.
        var response = await db.Services.RegisterPaymentAsync(
        [
            new ServicePayment(first.Kind, first.ServiceId, first.Total, true),
            new ServicePayment(second.Kind, second.ServiceId, 75m, false),
        ]);

        Assert.Equal(Response.Successful, response);

        var after = await db.Services.ListForPetSitterAsync(petSitterId);
        var settled = after.Single(s => s.Kind == ServiceKind.Walk);
        var partial = after.Single(s => s.Kind == ServiceKind.Sitting);

        Assert.True(settled.ServicePaid);
        Assert.Equal(100m, settled.Price);
        Assert.Equal(0m, settled.AmountDue);

        // The price survives: what the service cost is still on the record, and only the
        // settled/outstanding split moved.
        Assert.False(partial.ServicePaid);
        Assert.Equal(100m, partial.Price);
        Assert.Equal(100m, partial.Total);
        Assert.Equal(75m, partial.AmountSettled);
        Assert.Equal(25m, partial.Outstanding);
        Assert.Equal(25m, partial.AmountDue);
    }

    /// <summary>
    /// A service can be settled more than once — some credit when it was booked, cash later — so
    /// each write has to add to what is already recorded. Assigning instead would silently discard
    /// the earlier settlement and the tutor would be charged for it twice.
    /// </summary>
    [Fact]
    public async Task SettlementsAccumulateAcrossSeparatePayments()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 500m);

        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        // First 450 from the tutor's credit, then 30 in cash.
        await db.Services.RegisterPaymentAsync([new ServicePayment(service.Kind, service.ServiceId, 450m, false, 450m)]);
        await db.Services.RegisterPaymentAsync([new ServicePayment(service.Kind, service.ServiceId, 30m, false)]);

        var after = (await db.Services.GetAsync(petSitterId, ServiceKind.Walk, service.ServiceId))!;

        Assert.Equal(500m, after.Total);
        Assert.Equal(480m, after.AmountSettled);
        Assert.Equal(20m, after.Outstanding);

        // Only the credit-funded part is attributed to credit; the cash is not.
        Assert.Equal(450m, after.CreditApplied);
        Assert.False(after.ServicePaid);
    }

    /// <summary>
    /// A hotel's money column is PricePerDay, so a settlement must not touch it — dividing a
    /// remainder back into the nightly rate is exactly what the settled column replaced. The stay
    /// keeps totalling what it costs, and the payment shows up as the part already settled.
    /// </summary>
    [Fact]
    public async Task PartSettlingAStayKeepsItsNightlyRate()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(3),
            PricePerDay = 100m,
        });

        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        Assert.Equal(300m, stay.Total);

        await db.Services.RegisterPaymentAsync([new ServicePayment(stay.Kind, stay.ServiceId, 150m, false)]);

        var after = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.Equal(100m, after.Price);
        Assert.Equal(300m, after.Total);
        Assert.Equal(150m, after.AmountSettled);
        Assert.Equal(150m, after.Outstanding);
        Assert.False(after.ServicePaid);
    }

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public async Task APaymentCanSettleAnyKind(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, kind, petSitterId, dogId, August1, 100m);
        var service = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.RegisterPaymentAsync([new ServicePayment(kind, service.ServiceId, service.Price, true)]);

        var after = (await db.Services.GetAsync(petSitterId, kind, service.ServiceId))!;
        Assert.True(after.ServicePaid);
        Assert.Equal(0m, after.AmountDue);
    }

    [Fact]
    public async Task AnEmptyPaymentIsAcceptedAndChangesNothing()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 100m);

        Assert.Equal(Response.Successful, await db.Services.RegisterPaymentAsync([]));

        var after = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        Assert.False(after.ServicePaid);
        Assert.Equal(100m, after.Price);
    }

    /// <summary>Billing counts only what has been marked paid.</summary>
    [Fact]
    public async Task MonthlyIncomeCountsOnlyPaidServices()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 40m);
        await AddAsync(db, ServiceKind.Sitting, petSitterId, dogId, August1.AddDays(1), 80m);

        var services = await db.Services.ListForPetSitterAsync(petSitterId);
        var walk = services.Single(s => s.Kind == ServiceKind.Walk);
        await db.Services.SetPaidAsync(walk.Kind, walk.ServiceId, true);

        var income = await db.Services.GetMonthlyIncomeAsync(petSitterId, 2026, 8);

        Assert.Equal(40m, income.Walk);
        Assert.Equal(0m, income.Sitting);
        Assert.Equal(40m, income.Total);
    }

    [Fact]
    public async Task MonthlyIncomeIgnoresOtherMonths()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 40m);
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1.AddMonths(1), 90m);

        foreach (var service in await db.Services.ListForPetSitterAsync(petSitterId))
        {
            await db.Services.SetPaidAsync(service.Kind, service.ServiceId, true);
        }

        Assert.Equal(40m, (await db.Services.GetMonthlyIncomeAsync(petSitterId, 2026, 8)).Total);
        Assert.Equal(90m, (await db.Services.GetMonthlyIncomeAsync(petSitterId, 2026, 9)).Total);
        Assert.Equal(0m, (await db.Services.GetMonthlyIncomeAsync(petSitterId, 2026, 10)).Total);
    }

    /// <summary>
    /// A stay charges its nightly rate times the nights, plus a one-off extra for something like
    /// a late pick-up: 100 a night for 5 nights plus 40 comes to 540.
    /// </summary>
    [Fact]
    public async Task AHotelStayAddsItsExtraChargeOnTopOfTheNights()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(5),
            PricePerDay = 100m,
            ExtraCharge = 40m,
        });

        var booked = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal(5, booked.Nights);
        Assert.Equal(40m, booked.ExtraCharge);
        Assert.Equal(540m, booked.Total);

        // Not carried out yet, so nothing may be charged for it — the total is only what it will
        // come to once the stay has happened.
        Assert.Equal(0m, booked.AmountDue);
        Assert.Equal(540m, booked.AmountUpcoming);

        await db.Services.SetDoneAsync(ServiceKind.Hotel, booked.ServiceId, true);

        var stay = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, booked.ServiceId))!;
        Assert.Equal(540m, stay.AmountDue);
        Assert.Equal(0m, stay.AmountUpcoming);
    }

    [Fact]
    public async Task AStayWithNoExtraChargeTotalsJustTheNights()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(5),
            PricePerDay = 100m,
        });

        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal(0m, stay.ExtraCharge);
        Assert.Equal(500m, stay.Total);
    }

    [Fact]
    public async Task EditingAStayCanAddAndClearTheExtraCharge()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddAsync(db, ServiceKind.Hotel, petSitterId, dogId, August1, 100m);
        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        ServiceItem Edited(decimal extra) => new()
        {
            ServiceId = stay.ServiceId,
            Kind = ServiceKind.Hotel,
            DogId = dogId,
            DogName = stay.DogName,
            TutorName = stay.TutorName,
            Date = August1,
            EndDate = August1.AddDays(2),
            Price = 100m,
            ExtraCharge = extra,
        };

        await db.Services.UpdateAsync(Edited(40m));
        var withExtra = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.Equal(40m, withExtra.ExtraCharge);
        Assert.Equal(240m, withExtra.Total);

        await db.Services.UpdateAsync(Edited(0m));
        var without = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.Equal(0m, without.ExtraCharge);
        Assert.Equal(200m, without.Total);
    }

    /// <summary>
    /// The extra survives a part settlement. It used to be cleared, because the remainder was
    /// folded into the nightly rate and would otherwise have been charged twice; now nothing is
    /// rewritten, so wiping it would destroy part of what the stay cost.
    /// </summary>
    [Fact]
    public async Task PartSettlingAStayKeepsItsExtraCharge()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(2),
            PricePerDay = 100m,
            ExtraCharge = 40m,
        });

        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        Assert.Equal(240m, stay.Total);

        // 140 received against a 240 stay leaves 100 owing, recorded rather than repriced.
        await db.Services.RegisterPaymentAsync([new ServicePayment(stay.Kind, stay.ServiceId, 140m, false, 0m)]);

        var after = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.Equal(40m, after.ExtraCharge);
        Assert.Equal(100m, after.Price);
        Assert.Equal(240m, after.Total);
        Assert.Equal(140m, after.AmountSettled);
        Assert.Equal(100m, after.Outstanding);
        Assert.False(after.ServicePaid);
    }

    /// <summary>Settling a stay in full must leave its extra on the record.</summary>
    [Fact]
    public async Task SettlingAStayKeepsItsExtraCharge()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(2),
            PricePerDay = 100m,
            ExtraCharge = 40m,
        });

        var stay = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        await db.Services.RegisterPaymentAsync([new ServicePayment(stay.Kind, stay.ServiceId, stay.Price, true, stay.ExtraCharge)]);

        var after = (await db.Services.GetAsync(petSitterId, ServiceKind.Hotel, stay.ServiceId))!;
        Assert.True(after.ServicePaid);
        Assert.Equal(40m, after.ExtraCharge);
        Assert.Equal(240m, after.Total);
    }

    [Fact]
    public async Task GettingAServiceThatDoesNotExistReturnsNull()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();

        Assert.Null(await db.Services.GetAsync(petSitterId, ServiceKind.Walk, 999));
    }

    /// <summary>Sequential, not WhenAll: concurrent writers on one SQLite file invite lock errors.</summary>
    private static async Task AddOneOfEachAsync(TestDatabase db, int petSitterId, int dogId)
    {
        await AddAsync(db, ServiceKind.Walk, petSitterId, dogId, August1, 40m);
        await AddAsync(db, ServiceKind.Sitting, petSitterId, dogId, August1.AddDays(1), 80m);
        await AddAsync(db, ServiceKind.Hotel, petSitterId, dogId, August1.AddDays(2), 100m);
        await AddAsync(db, ServiceKind.DayCare, petSitterId, dogId, August1.AddDays(6), 60m);
    }

    private static Task AddAsync(TestDatabase db, ServiceKind kind, int petSitterId, int dogId, DateTime date, decimal price) => kind switch
    {
        ServiceKind.Walk => db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = date,
            Price = price,
        }),
        ServiceKind.Sitting => db.Services.AddSittingAsync(new PetSittingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = date,
            Price = price,
        }),
        ServiceKind.Hotel => db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = date,
            EndDate = date.AddDays(1),
            PricePerDay = price,
        }),
        ServiceKind.DayCare => db.Services.AddDayCareAsync(new DayCareService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = date,
            Price = price,
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Fact]
    public async Task ADiscountSurvivesTheRoundTripOnEveryKind()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = August1, Price = 40m, Discount = 10m,
        });
        await db.Services.AddSittingAsync(new PetSittingService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = August1, Price = 100m, Discount = 25m,
        });
        await db.Services.AddHotelAsync(new PetHotelService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            StartDate = August1,
            EndDate = August1.AddDays(2),
            PricePerDay = 80m,
            Discount = 50m,
        });
        await db.Services.AddDayCareAsync(new DayCareService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = August1, Price = 60m, Discount = 5m,
        });

        var services = await db.Services.ListForPetSitterAsync(petSitterId);

        Assert.Equal(10m, services.Single(s => s.Kind == ServiceKind.Walk).Discount);
        Assert.Equal(25m, services.Single(s => s.Kind == ServiceKind.Sitting).Discount);
        Assert.Equal(50m, services.Single(s => s.Kind == ServiceKind.Hotel).Discount);
        Assert.Equal(5m, services.Single(s => s.Kind == ServiceKind.DayCare).Discount);

        // 160 for two nights, halved.
        Assert.Equal(80m, services.Single(s => s.Kind == ServiceKind.Hotel).Total);
    }

    [Fact]
    public async Task EditingAServiceSavesItsDiscount()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddSittingAsync(new PetSittingService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = August1, Price = 100m,
        });

        var booked = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        Assert.Equal(100m, booked.Total);

        await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = booked.ServiceId,
            Kind = booked.Kind,
            DogId = booked.DogId,
            DogName = booked.DogName,
            TutorName = booked.TutorName,
            Date = booked.Date,
            Price = booked.Price,
            Discount = 30m,
        });

        var edited = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal(30m, edited.Discount);
        Assert.Equal(70m, edited.Total);
    }

    [Fact]
    public async Task ADiscountedBookingIsBilledAtItsDiscountedPrice()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        await db.Services.AddSittingAsync(new PetSittingService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = August1, Price = 200m, Discount = 25m,
        });

        var booked = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();
        await db.Services.SetDoneAsync(booked.Kind, booked.ServiceId, true);

        var done = (await db.Services.ListForPetSitterAsync(petSitterId)).Single();

        Assert.Equal(150m, done.AmountDue);
    }

    /// <summary>
    /// The reported failure, written down: several hotel stays booked, one of them re-dated, and
    /// every other booking still there afterwards with the values it was given.
    /// </summary>
    /// <remarks>
    /// Three dogs, because the stays that went missing were a whole tutor's worth booked together
    /// — one row per dog over the same nights, which is the shape that would be lost as a group if
    /// an edit ever matched on anything but the primary key.
    /// </remarks>
    [Fact]
    public async Task ChangingOneStaysDatesLeavesEveryOtherStayAlone()
    {
        using var db = new TestDatabase();
        var petSitterId = await db.SeedPetSitterAsync();
        var tutorId = await db.SeedTutorAsync(petSitterId, "Marcos");

        var luna = await db.SeedDogAsync(tutorId, "Luna");
        var noah = await db.SeedDogAsync(tutorId, "Noah");
        var pipoca = await db.SeedDogAsync(tutorId, "Pipoca");

        foreach (var (dogId, rate) in new[] { (luna, 50m), (noah, 90m), (pipoca, 50m) })
        {
            await db.Services.AddHotelAsync(new PetHotelService
            {
                DogId = dogId,
                PetSitterId = petSitterId,
                StartDate = August1,
                EndDate = August1.AddDays(2),
                PricePerDay = rate,
                ExtraCharge = 25m,
            });
        }

        var stays = await db.Services.ListForPetSitterAsync(petSitterId);
        Assert.Equal(3, stays.Length);

        var moved = stays.Single(s => s.DogId == noah);
        var response = await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = moved.ServiceId,
            Kind = moved.Kind,
            DogId = moved.DogId,
            DogName = moved.DogName,
            TutorName = moved.TutorName,
            Date = August1.AddDays(8),
            EndDate = August1.AddDays(10),
            Price = moved.Price,
            ExtraCharge = moved.ExtraCharge,
        });

        Assert.Equal(Response.Successful, response);

        var after = await db.Services.ListForPetSitterAsync(petSitterId);
        Assert.Equal(3, after.Length);

        var edited = after.Single(s => s.DogId == noah);
        Assert.Equal(August1.AddDays(8), edited.Date);
        Assert.Equal(August1.AddDays(10), edited.EndDate);

        // The two that were not edited, unchanged down to the extra charge.
        foreach (var untouched in after.Where(s => s.DogId != noah))
        {
            Assert.Equal(August1, untouched.Date);
            Assert.Equal(August1.AddDays(2), untouched.EndDate);
            Assert.Equal(50m, untouched.Price);
            Assert.Equal(25m, untouched.ExtraCharge);
        }
    }

    /// <summary>The same guarantee for every kind, and across kinds: an edit is one row.</summary>
    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public async Task AnEditTouchesOneBookingAndNoOther(ServiceKind kind)
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();

        // Two of the kind being edited, plus one of each of the others.
        await AddOneOfEachAsync(db, petSitterId, dogId);
        await AddOneOfEachAsync(db, petSitterId, dogId);

        var before = await db.Services.ListForPetSitterAsync(petSitterId);
        Assert.Equal(8, before.Length);

        var target = before.First(s => s.Kind == kind);
        await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = target.ServiceId,
            Kind = target.Kind,
            DogId = target.DogId,
            DogName = target.DogName,
            TutorName = target.TutorName,
            Date = August1.AddDays(20),
            EndDate = kind == ServiceKind.Hotel ? August1.AddDays(22) : null,
            Price = 777m,
        });

        var after = await db.Services.ListForPetSitterAsync(petSitterId);

        Assert.Equal(8, after.Length);
        Assert.Single(after, s => s.Price == 777m);

        foreach (var untouched in before.Where(s => s.ServiceId != target.ServiceId || s.Kind != target.Kind))
        {
            var same = after.Single(s => s.Kind == untouched.Kind && s.ServiceId == untouched.ServiceId);
            Assert.Equal(untouched.Date, same.Date);
            Assert.Equal(untouched.Price, same.Price);
        }
    }

    /// <summary>
    /// Editing a booking that is no longer there is refused rather than reported as saved.
    /// </summary>
    /// <remarks>
    /// It happens when the same record is open on two screens and one of them deletes it. Saying
    /// "saved" would leave the sitter looking at values nothing kept.
    /// </remarks>
    [Fact]
    public async Task EditingABookingThatIsGoneFails()
    {
        using var db = new TestDatabase();
        var (petSitterId, _, dogId) = await db.SeedAccountAsync();
        await AddOneOfEachAsync(db, petSitterId, dogId);

        var walk = (await db.Services.ListForPetSitterAsync(petSitterId)).Single(s => s.Kind == ServiceKind.Walk);
        await db.Services.DeleteAsync(ServiceKind.Walk, walk.ServiceId);

        var response = await db.Services.UpdateAsync(new ServiceItem
        {
            ServiceId = walk.ServiceId,
            Kind = ServiceKind.Walk,
            DogId = walk.DogId,
            DogName = walk.DogName,
            TutorName = walk.TutorName,
            Date = August1.AddDays(1),
            Price = 60m,
        });

        Assert.Equal(Response.Failed, response);
        Assert.Equal(3, (await db.Services.ListForPetSitterAsync(petSitterId)).Length);
    }
}
