using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The payment ledger: what a payment writes, and — the reason it exists — what taking one back
/// out has to put right.
/// </summary>
public class RepositoryPaymentsTests
{
    private static readonly DateTime August1 = new(2026, 8, 1, 9, 0, 0);

    [Fact]
    public async Task APaymentSettlesTheServicesItCovers()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);

        await PayAsync(db, petSitterId, tutorId, 100m);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.Equal(100m, service.AmountSettled);
        Assert.True(service.ServicePaid);
        Assert.Equal(0m, service.AmountDue);
    }

    [Fact]
    public async Task WhatAPaymentCannotSpendBecomesCredit()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);

        await PayAsync(db, petSitterId, tutorId, 150m);

        var tutor = await db.Tutors.GetAsync(tutorId);
        Assert.Equal(50m, tutor!.Credit);
    }

    [Fact]
    public async Task APaymentIsReadBackWithWhatItSettled()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);

        await PayAsync(db, petSitterId, tutorId, 150m);

        var payment = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        Assert.Equal(150m, payment.Amount);
        Assert.Equal(50m, payment.CreditStored);

        var allocation = Assert.Single(payment.Allocations);
        Assert.Equal(ServiceKind.Walk, allocation.Kind);
        Assert.Equal(100m, allocation.Amount);
    }

    [Fact]
    public async Task DeletingAPaymentPutsTheServiceBackInDebt()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);
        await PayAsync(db, petSitterId, tutorId, 100m);

        var payment = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        Assert.Equal(Response.Successful, await db.Payments.DeleteAsync(payment.TutorPaymentId));

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.Equal(0m, service.AmountSettled);
        Assert.False(service.ServicePaid);
        Assert.Equal(100m, service.AmountDue);
        Assert.Empty(await db.Payments.ListForTutorAsync(tutorId));
    }

    /// <summary>
    /// The service was settled twice. Undoing one payment must leave the other's share alone
    /// rather than clearing the service down to nothing.
    /// </summary>
    [Fact]
    public async Task DeletingOneOfTwoPaymentsKeepsTheOthersShare()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);

        await PayAsync(db, petSitterId, tutorId, 40m);
        await PayAsync(db, petSitterId, tutorId, 60m);

        var first = (await db.Payments.ListForTutorAsync(tutorId)).Single(p => p.Amount == 40m);
        await db.Payments.DeleteAsync(first.TutorPaymentId);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.Equal(60m, service.AmountSettled);
        Assert.False(service.ServicePaid);
        Assert.Equal(40m, service.AmountDue);
    }

    [Fact]
    public async Task DeletingAnAdvanceTakesTheCreditBack()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, _) = await db.SeedAccountAsync();

        await PayAsync(db, petSitterId, tutorId, 500m);
        Assert.Equal(500m, (await db.Tutors.GetAsync(tutorId))!.Credit);

        var payment = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        await db.Payments.DeleteAsync(payment.TutorPaymentId);

        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    /// <summary>
    /// The awkward case the ledger exists for: 500 typed by mistake became credit, the credit was
    /// then spent on a booking, and only afterwards was the mistake noticed. The tutor no longer
    /// holds the money, so undoing the payment has to follow it into the service it went to.
    /// </summary>
    [Fact]
    public async Task DeletingAnAdvanceAlreadySpentReclaimsItFromTheServices()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();

        await PayAsync(db, petSitterId, tutorId, 500m);

        // A booking made afterwards, settled out of the credit the way CreditSpender does it.
        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = August1,
            Price = 300m,
            ServicePaid = false,
            ServiceDone = false,
        });

        var booked = await db.Services.ListForTutorAsync(petSitterId, tutorId);
        var (spent, applied) = PaymentAllocation.AllocateCredit(booked, 500m);
        await db.Services.RegisterPaymentAsync(spent);
        await db.Tutors.SetCreditAsync(tutorId, 500m - applied);

        Assert.Equal(200m, (await db.Tutors.GetAsync(tutorId))!.Credit);

        var payment = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        Assert.Equal(Response.Successful, await db.Payments.DeleteAsync(payment.TutorPaymentId));

        // 200 came off the balance; the other 300 had to be taken back off the booking it paid for.
        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.Equal(0m, service.AmountSettled);
        Assert.Equal(0m, service.CreditApplied);
        Assert.False(service.ServicePaid);
    }

    /// <summary>
    /// Correcting is a delete followed by a fresh payment — the flow the tutor screen runs — so a
    /// 500 typed for 50 has to let go of everything the larger amount reached.
    /// </summary>
    [Fact]
    public async Task CorrectingDownwardsReleasesTheServicesTheLargerAmountSettled()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m, August1);
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m, August1.AddDays(1));

        await PayAsync(db, petSitterId, tutorId, 500m);
        Assert.Equal(300m, (await db.Tutors.GetAsync(tutorId))!.Credit);

        var mistake = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        await db.Payments.DeleteAsync(mistake.TutorPaymentId);
        await PayAsync(db, petSitterId, tutorId, 50m);

        var services = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).OrderBy(s => s.Date).ToArray();

        Assert.Equal(50m, services[0].AmountSettled);
        Assert.False(services[0].ServicePaid);
        Assert.Equal(0m, services[1].AmountSettled);
        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);
        Assert.Equal(150m, services.Sum(s => s.AmountDue));
    }

    /// <summary>
    /// A payment settles a booking that has not been carried out yet, and nothing spills into
    /// credit. Paying up front is the ordinary case, and the sitter should not have to tick the
    /// work done before the money lands on it.
    /// </summary>
    /// <remarks>
    /// The service is still not billable — <see cref="ServiceItem.AmountDue"/> stays zero until it
    /// is executed. Settling it and being allowed to ask for it are two different questions.
    /// </remarks>
    [Fact]
    public async Task APaymentSettlesWorkThatHasNotBeenCarriedOut()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = August1,
            Price = 100m,
            ServicePaid = false,
            ServiceDone = false,
        });

        await PayAsync(db, petSitterId, tutorId, 100m);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.Equal(100m, service.AmountSettled);
        Assert.True(service.ServicePaid);
        Assert.Equal(0m, service.Outstanding);

        // Nothing was left over, so no credit was banked.
        Assert.Equal(0m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    /// <summary>Only what the services could not absorb is held as credit.</summary>
    [Fact]
    public async Task OnlyTheSurplusBecomesCredit()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();

        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = August1,
            Price = 100m,
            ServicePaid = false,
            ServiceDone = false,
        });

        await PayAsync(db, petSitterId, tutorId, 250m);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        Assert.True(service.ServicePaid);
        Assert.Equal(150m, (await db.Tutors.GetAsync(tutorId))!.Credit);
    }

    [Fact]
    public async Task DeletingATutorTakesTheirPaymentsWithThem()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);
        await PayAsync(db, petSitterId, tutorId, 100m);

        await db.Tutors.Delete(tutorId);

        Assert.Empty(await db.Payments.ListForTutorAsync(tutorId));
    }

    /// <summary>Undoing a payment cannot unsettle a service that has since been deleted.</summary>
    [Fact]
    public async Task DeletingAPaymentSurvivesAServiceDeletedSinceItWasTaken()
    {
        using var db = new TestDatabase();
        var (petSitterId, tutorId, dogId) = await db.SeedAccountAsync();
        await AddDoneWalkAsync(db, petSitterId, dogId, 100m);
        await PayAsync(db, petSitterId, tutorId, 100m);

        var service = (await db.Services.ListForTutorAsync(petSitterId, tutorId)).Single();
        await db.Services.DeleteAsync(service.Kind, service.ServiceId);

        var payment = (await db.Payments.ListForTutorAsync(tutorId)).Single();
        Assert.Empty(payment.Allocations);
        Assert.Equal(Response.Successful, await db.Payments.DeleteAsync(payment.TutorPaymentId));
        Assert.Empty(await db.Payments.ListForTutorAsync(tutorId));
    }

    private static async Task AddDoneWalkAsync(TestDatabase db, int petSitterId, int dogId, decimal price, DateTime? date = null)
    {
        await db.Services.AddWalkAsync(new WalkingService
        {
            DogId = dogId,
            PetSitterId = petSitterId,
            Date = date ?? August1,
            Price = price,
            ServicePaid = false,
            ServiceDone = true,
        });
    }

    /// <summary>
    /// What the tutor screen does when a payment is confirmed: spread it over what is chargeable,
    /// bank the rest, and record both as one entry.
    /// </summary>
    private static async Task PayAsync(TestDatabase db, int petSitterId, int tutorId, decimal amount)
    {
        var services = await db.Services.ListForTutorAsync(petSitterId, tutorId);
        var (allocations, applied) = PaymentAllocation.Allocate(services, amount);

        var written = await db.Payments.RecordAsync(new TutorPayment
        {
            TutorId = tutorId,
            PetSitterId = petSitterId,
            Date = DateTime.Now,
            Amount = amount,
            CreditStored = amount - applied,
            Allocations = allocations,
        });

        Assert.Equal(Response.Successful, written);
    }
}
