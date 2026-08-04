using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// How money received is spread over what a tutor owes. No database — the rule is a pure
/// calculation, and it is where "you can only charge for work that has been carried out" is
/// actually enforced.
/// </summary>
public class PaymentAllocationTests
{
    private static readonly DateTime August1 = new(2026, 8, 1, 9, 0, 0);

    /// <summary>An advance is not a payment: with nothing executed there is nothing to settle.</summary>
    [Fact]
    public void NothingExecutedMeansNothingIsAllocated()
    {
        var services = new[]
        {
            Service(1, ServiceKind.Walk, August1, 100m),
            Service(2, ServiceKind.Sitting, August1.AddDays(1), 100m),
        };

        var (payments, applied) = PaymentAllocation.Allocate(services, 500m);

        Assert.Empty(payments);
        Assert.Equal(0m, applied);
    }

    /// <summary>
    /// The executed one is settled and the booking still to come is left alone, however much money
    /// arrived — the sitter has not earned it yet.
    /// </summary>
    [Fact]
    public void OnlyExecutedServicesAreSettled()
    {
        var services = new[]
        {
            Service(1, ServiceKind.Walk, August1, 100m, done: true),
            Service(2, ServiceKind.Sitting, August1.AddDays(1), 100m),
        };

        var (payments, applied) = PaymentAllocation.Allocate(services, 500m);

        Assert.Single(payments);
        Assert.Equal(1, payments[0].ServiceId);
        Assert.True(payments[0].FullyPaid);
        Assert.Equal(100m, applied);
    }

    /// <summary>
    /// Age does not override execution: an old booking that never happened is skipped in favour of
    /// a newer one that did.
    /// </summary>
    [Fact]
    public void AnOlderUnexecutedServiceDoesNotJumpTheQueue()
    {
        var services = new[]
        {
            Service(1, ServiceKind.Walk, August1, 100m),
            Service(2, ServiceKind.Sitting, August1.AddDays(10), 100m, done: true),
        };

        var (payments, applied) = PaymentAllocation.Allocate(services, 100m);

        Assert.Single(payments);
        Assert.Equal(2, payments[0].ServiceId);
        Assert.Equal(100m, applied);
    }

    /// <summary>Among executed work, the longest-outstanding debt is still cleared first.</summary>
    [Fact]
    public void ExecutedServicesAreStillSettledOldestFirst()
    {
        var services = new[]
        {
            Service(2, ServiceKind.Sitting, August1.AddDays(5), 100m, done: true),
            Service(1, ServiceKind.Walk, August1, 100m, done: true),
        };

        var (payments, applied) = PaymentAllocation.Allocate(services, 150m);

        Assert.Equal(2, payments.Count);
        Assert.Equal(1, payments[0].ServiceId);
        Assert.True(payments[0].FullyPaid);
        Assert.Equal(2, payments[1].ServiceId);
        Assert.False(payments[1].FullyPaid);
        Assert.Equal(50m, payments[1].Amount);
        Assert.Equal(150m, applied);
    }

    /// <summary>
    /// What is left over is what the caller turns into credit, so the applied figure has to stop at
    /// the executed work rather than swallowing the advance.
    /// </summary>
    [Fact]
    public void MoneyBeyondTheExecutedWorkIsLeftForTheCallerToBank()
    {
        var services = new[] { Service(1, ServiceKind.Walk, August1, 100m, done: true) };

        var (payments, applied) = PaymentAllocation.Allocate(services, 250m);

        Assert.Single(payments);
        Assert.Equal(100m, applied);
        Assert.Equal(150m, 250m - applied);
    }

    /// <summary>Already-settled work is not charged for twice.</summary>
    [Fact]
    public void PaidServicesAreSkipped()
    {
        var services = new[]
        {
            Service(1, ServiceKind.Walk, August1, 100m, done: true, paid: true),
            Service(2, ServiceKind.Sitting, August1.AddDays(1), 100m, done: true),
        };

        var (payments, applied) = PaymentAllocation.Allocate(services, 100m);

        Assert.Single(payments);
        Assert.Equal(2, payments[0].ServiceId);
        Assert.Equal(100m, applied);
    }

    /// <summary>
    /// A part-paid stay records what was settled and keeps its nightly rate. The amount reported is
    /// the money itself, not a rate derived from it — the stay is still worth what it costs.
    /// </summary>
    [Fact]
    public void APartPaidStayRecordsTheAmountNotAReducedRate()
    {
        var stay = Service(1, ServiceKind.Hotel, August1, 100m, done: true, end: August1.AddDays(3));

        var (payments, applied) = PaymentAllocation.Allocate([stay], 150m);

        Assert.Single(payments);
        Assert.False(payments[0].FullyPaid);
        Assert.Equal(150m, payments[0].Amount);
        Assert.Equal(0m, payments[0].FromCredit);
        Assert.Equal(150m, applied);
    }

    /// <summary>
    /// Only what is still outstanding is allocated, so settling the same service twice cannot
    /// collect more than it costs.
    /// </summary>
    [Fact]
    public void AnAlreadyPartSettledServiceOnlyTakesItsRemainder()
    {
        var service = Service(1, ServiceKind.Walk, August1, 100m, done: true, settled: 75m);

        var (payments, applied) = PaymentAllocation.Allocate([service], 100m);

        Assert.Single(payments);
        Assert.Equal(25m, payments[0].Amount);
        Assert.True(payments[0].FullyPaid);
        Assert.Equal(25m, applied);
    }

    /// <summary>
    /// The rule that separates credit from a payment. Credit is money already handed over, so it
    /// settles a booking that has not happened yet — which <see cref="PaymentAllocation.Allocate"/>
    /// refuses to do.
    /// </summary>
    [Fact]
    public void CreditSettlesWorkThatHasNotBeenCarriedOut()
    {
        var booked = new[] { Service(1, ServiceKind.Walk, August1, 500m) };

        var (payments, applied) = PaymentAllocation.AllocateCredit(booked, 450m);

        Assert.Single(payments);
        Assert.Equal(450m, payments[0].Amount);
        Assert.Equal(450m, payments[0].FromCredit);
        Assert.False(payments[0].FullyPaid);
        Assert.Equal(450m, applied);

        // A payment of the same size would settle nothing at all here.
        Assert.Empty(PaymentAllocation.Allocate(booked, 450m).Payments);
    }

    /// <summary>Credit beyond what the bookings cost stays with the tutor.</summary>
    [Fact]
    public void CreditBeyondTheBookingsIsLeftOver()
    {
        var booked = new[] { Service(1, ServiceKind.Walk, August1, 300m) };

        var (payments, applied) = PaymentAllocation.AllocateCredit(booked, 450m);

        Assert.Single(payments);
        Assert.True(payments[0].FullyPaid);
        Assert.Equal(300m, applied);
        Assert.Equal(150m, 450m - applied);
    }

    /// <summary>Credit does not settle work that has already been paid for.</summary>
    [Fact]
    public void CreditSkipsPaidServices()
    {
        var services = new[]
        {
            Service(1, ServiceKind.Walk, August1, 100m, paid: true),
            Service(2, ServiceKind.Sitting, August1.AddDays(1), 100m),
        };

        var (payments, applied) = PaymentAllocation.AllocateCredit(services, 500m);

        Assert.Single(payments);
        Assert.Equal(2, payments[0].ServiceId);
        Assert.Equal(100m, applied);
    }

    private static ServiceItem Service(
        int id,
        ServiceKind kind,
        DateTime date,
        decimal price,
        bool done = false,
        bool paid = false,
        DateTime? end = null,
        decimal settled = 0m) => new()
        {
            ServiceId = id,
            Kind = kind,
            DogId = 1,
            DogName = "Toby",
            TutorName = "Ana",
            Date = date,
            EndDate = end,
            Price = price,
            ServiceDone = done,
            ServicePaid = paid,
            AmountSettled = settled,
        };
}