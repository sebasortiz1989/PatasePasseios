using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The money rules on <see cref="ServiceItem"/>. No database — these are pure calculations, and
/// they are where a hotel stay stops being a nightly rate and becomes an amount owed.
/// </summary>
public class ServiceItemTests
{
    private static ServiceItem Service(
        ServiceKind kind,
        decimal price,
        DateTime? end = null,
        bool paid = false,
        bool done = false) => new()
        {
            ServiceId = 1,
            Kind = kind,
            DogId = 1,
            DogName = "Toby",
            TutorName = "Ana",
            Date = new DateTime(2026, 8, 1, 9, 0, 0),
            EndDate = end,
            Price = price,
            ServicePaid = paid,
            ServiceDone = done,
        };

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.DayCare)]
    public void FlatFeeKindsTotalTheirPrice(ServiceKind kind)
    {
        var service = Service(kind, 100m);

        Assert.Equal(1, service.Nights);
        Assert.Equal(100m, service.Total);
    }

    [Fact]
    public void HotelStayTotalsTheRateTimesTheNights()
    {
        var stay = Service(ServiceKind.Hotel, 100m, end: new DateTime(2026, 8, 4, 9, 0, 0));

        Assert.Equal(3, stay.Nights);
        Assert.Equal(300m, stay.Total);
    }

    /// <summary>
    /// Checking in and out on the same day still bills a night. Without this a same-day stay would
    /// total nothing, which is neither how a day rate is charged nor something the screens expect.
    /// </summary>
    [Fact]
    public void SameDayHotelStayStillBillsOneNight()
    {
        var stay = Service(ServiceKind.Hotel, 80m, end: new DateTime(2026, 8, 1, 18, 0, 0));

        Assert.Equal(1, stay.Nights);
        Assert.Equal(80m, stay.Total);
    }

    /// <summary>Times of day must not round a stay up to an extra night.</summary>
    [Fact]
    public void HotelNightsCountWholeDaysNotElapsedHours()
    {
        var stay = Service(ServiceKind.Hotel, 50m, end: new DateTime(2026, 8, 3, 8, 0, 0));

        Assert.Equal(2, stay.Nights);
        Assert.Equal(100m, stay.Total);
    }

    /// <summary>Executed and unpaid is the one state in which the whole total may be charged.</summary>
    [Fact]
    public void AnExecutedUnpaidServiceOwesItsWholeTotal()
    {
        var stay = Service(ServiceKind.Hotel, 100m, end: new DateTime(2026, 8, 3, 9, 0, 0), done: true);

        Assert.Equal(200m, stay.AmountDue);
        Assert.Equal(0m, stay.AmountUpcoming);
    }

    /// <summary>
    /// Work that has not been carried out is worth nothing yet, however much it will eventually
    /// cost — the sitter cannot bill for a walk that has not happened.
    /// </summary>
    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public void AnUnexecutedServiceOwesNothingButCountsAsUpcoming(ServiceKind kind)
    {
        var service = Service(kind, 100m, end: new DateTime(2026, 8, 3, 9, 0, 0));

        Assert.Equal(0m, service.AmountDue);
        Assert.Equal(service.Total, service.AmountUpcoming);
    }

    [Theory]
    [InlineData(ServiceKind.Walk)]
    [InlineData(ServiceKind.Sitting)]
    [InlineData(ServiceKind.Hotel)]
    [InlineData(ServiceKind.DayCare)]
    public void APaidServiceOwesNothing(ServiceKind kind)
    {
        var service = Service(kind, 100m, end: new DateTime(2026, 8, 5, 9, 0, 0), paid: true, done: true);

        Assert.Equal(0m, service.AmountDue);
        Assert.Equal(0m, service.AmountUpcoming);
    }
}