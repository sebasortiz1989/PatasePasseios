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
    /// A part-paid stay carries its remainder in the nightly rate, so the figure written is the
    /// remainder divided over the nights rather than the remainder itself.
    /// </summary>
    [Fact]
    public void APartPaidStayCarriesItsRemainderInTheNightlyRate()
    {
        var stay = Service(1, ServiceKind.Hotel, August1, 100m, done: true, end: August1.AddDays(3));

        var (payments, applied) = PaymentAllocation.Allocate([stay], 150m);

        Assert.Single(payments);
        Assert.False(payments[0].FullyPaid);
        Assert.Equal(150m, payments[0].Amount);
        Assert.Equal(0m, payments[0].FromCredit);
        Assert.Equal(150m, applied);
    }

    private static ServiceItem Service(
        int id,
        ServiceKind kind,
        DateTime date,
        decimal price,
        bool done = false,
        bool paid = false,
        DateTime? end = null) => new()
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
        };
}