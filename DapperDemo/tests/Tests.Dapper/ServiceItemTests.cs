using PatasePasseios.Repository.Dapper.Dtos;
using System.Linq;
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
        bool done = false,
        decimal settled = 0m,
        decimal credit = 0m,
        decimal discount = 0m,
        decimal extra = 0m,
        DateTime? start = null) => new()
        {
            ServiceId = 1,
            Kind = kind,
            DogId = 1,
            DogName = "Toby",
            TutorName = "Ana",
            Date = start ?? new DateTime(2026, 8, 1, 9, 0, 0),
            EndDate = end,
            Price = price,
            ExtraCharge = extra,
            Discount = discount,
            ServicePaid = paid,
            ServiceDone = done,
            AmountSettled = settled,
            CreditApplied = credit,
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

    /// <summary>
    /// A part-settled service keeps its price. What it cost and what is left on it are two separate
    /// figures now, which is the whole point of the settled column.
    /// </summary>
    [Fact]
    public void SettlingPartOfAServiceLeavesTheRestOutstanding()
    {
        var service = Service(ServiceKind.Walk, 500m, settled: 450m);

        Assert.Equal(500m, service.Total);
        Assert.Equal(50m, service.Outstanding);
    }

    /// <summary>Over-settling cannot turn into a negative balance the sitter would owe back here.</summary>
    [Fact]
    public void SettlingMoreThanTheTotalDoesNotGoNegative()
    {
        var service = Service(ServiceKind.Walk, 100m, settled: 130m);

        Assert.Equal(0m, service.Outstanding);
    }

    /// <summary>Once executed, only the unsettled part may be charged — not the whole price again.</summary>
    [Fact]
    public void AnExecutedPartSettledServiceOwesOnlyTheRemainder()
    {
        var service = Service(ServiceKind.Walk, 500m, done: true, settled: 450m);

        Assert.Equal(50m, service.AmountDue);
        Assert.Equal(0m, service.AmountUpcoming);
    }

    /// <summary>
    /// The scenario credit exists for: a booking settled from credit before it has happened. It owes
    /// nothing today because it has not been carried out, and what it will be worth is only the part
    /// the credit did not cover.
    /// </summary>
    [Fact]
    public void ABookingSettledFromCreditIsUpcomingOnlyForItsRemainder()
    {
        var service = Service(ServiceKind.Walk, 500m, settled: 450m, credit: 450m);

        Assert.Equal(0m, service.AmountDue);
        Assert.Equal(50m, service.AmountUpcoming);
        Assert.Equal(450m, service.CreditApplied);
    }

    /// <summary>A service settled in full by credit is worth nothing further, executed or not.</summary>
    [Fact]
    public void ABookingFullySettledFromCreditOwesNothing()
    {
        var service = Service(ServiceKind.Walk, 300m, paid: true, settled: 300m, credit: 300m);

        Assert.Equal(0m, service.Outstanding);
        Assert.Equal(0m, service.AmountDue);
        Assert.Equal(0m, service.AmountUpcoming);
    }

    [Fact]
    public void NoDiscountLeavesTheTotalAlone()
    {
        var service = Service(ServiceKind.Sitting, 100m);

        Assert.Equal(0m, service.DiscountAmount);
        Assert.Equal(100m, service.Subtotal);
        Assert.Equal(100m, service.Total);
    }

    [Fact]
    public void ADiscountComesOffTheTotal()
    {
        var service = Service(ServiceKind.Sitting, 100m, discount: 10m);

        Assert.Equal(100m, service.Subtotal);
        Assert.Equal(10m, service.DiscountAmount);
        Assert.Equal(90m, service.Total);
    }

    [Fact]
    public void AStayIsDiscountedOnNightsTimesRatePlusExtras()
    {
        // Three nights at 80 plus a 30 late pick-up is 270; 10% off is 27.
        var service = Service(
            ServiceKind.Hotel,
            80m,
            end: new DateTime(2026, 8, 4, 9, 0, 0),
            discount: 10m,
            extra: 30m);

        Assert.Equal(270m, service.Subtotal);
        Assert.Equal(27m, service.DiscountAmount);
        Assert.Equal(243m, service.Total);
    }

    [Fact]
    public void TheDiscountIsRoundedToCentavos()
    {
        // 15% of 33.33 is 4.9995, which must not reach the bill as a third of a centavo.
        var service = Service(ServiceKind.Sitting, 33.33m, discount: 15m);

        Assert.Equal(5.00m, service.DiscountAmount);
        Assert.Equal(28.33m, service.Total);
    }

    [Fact]
    public void AFullDiscountMakesTheServiceFree()
    {
        var service = Service(ServiceKind.Walk, 40m, discount: 100m, done: true);

        Assert.Equal(0m, service.Total);
        Assert.Equal(0m, service.AmountDue);
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(150)]
    public void ANonsenseRateCannotPushTheTotalOutOfRange(decimal discount)
    {
        // Nothing stops a hand-edited database holding these; the total must stay between free
        // and full price either way.
        var service = Service(ServiceKind.Sitting, 100m, discount: discount);

        Assert.InRange(service.Total, 0m, 100m);
    }

    [Fact]
    public void WhatIsBilledIsTheDiscountedAmount()
    {
        // The point of the whole change: everything downstream reads Total, so an executed and
        // unpaid discounted booking is owed at its discounted price.
        var service = Service(ServiceKind.Sitting, 200m, done: true, discount: 25m);

        Assert.Equal(150m, service.Outstanding);
        Assert.Equal(150m, service.AmountDue);
    }

    [Fact]
    public void SettlingADiscountedServiceClearsItAtTheDiscountedPrice()
    {
        var service = Service(ServiceKind.Sitting, 200m, done: true, discount: 25m, settled: 150m);

        Assert.Equal(0m, service.Outstanding);
        Assert.Equal(0m, service.AmountDue);
    }

    [Fact]
    public void AFlatFeeServiceIsBilledOnItsOwnDate()
    {
        var service = Service(ServiceKind.Walk, 60m);

        Assert.Equal(service.Date, service.BillingDate);
    }

    [Fact]
    public void AStayIsBilledOnItsCheckOut()
    {
        // 29 July to 2 August is one piece of work that finishes in August, billed once. All of it
        // is August's money — not split across the two months, and not July's because that is when
        // the dog arrived.
        var service = Service(
            ServiceKind.Hotel,
            100m,
            end: new DateTime(2026, 8, 2, 10, 0, 0),
            start: new DateTime(2026, 7, 29, 9, 0, 0));

        Assert.Equal(8, service.BillingDate.Month);
        Assert.Equal(2026, service.BillingDate.Year);
    }

    [Fact]
    public void AStayCrossingNewYearIsBilledInTheYearItEnds()
    {
        var service = Service(
            ServiceKind.Hotel,
            100m,
            end: new DateTime(2027, 1, 3, 10, 0, 0),
            start: new DateTime(2026, 12, 28, 9, 0, 0));

        Assert.Equal(2027, service.BillingDate.Year);
        Assert.Equal(1, service.BillingDate.Month);
    }

    /// <summary>
    /// The bug this rule exists for: a bill's "Já pago" and "A pagar" have to account for the whole
    /// month between them. Marcos Bernardi's August 2026 bill was R$ 945,00 of work against a
    /// R$ 500,00 payment, and the receipt read R$ 440,00 paid and R$ 445,00 owed — R$ 60,00 of his
    /// money, the part covering a service it could not fully clear, appeared on neither line.
    /// </summary>
    [Fact]
    public void ReceivedAndDueAccountForTheWholeTotal()
    {
        var cleared = Service(ServiceKind.Hotel, 315m, paid: true, done: true, settled: 315m);
        var partly = Service(ServiceKind.Hotel, 125m, done: true, settled: 60m);
        var untouched = Service(ServiceKind.Hotel, 180m, done: true);

        Assert.Equal(315m, cleared.AmountReceived);
        Assert.Equal(60m, partly.AmountReceived);
        Assert.Equal(0m, untouched.AmountReceived);

        ServiceItem[] bill = [cleared, partly, untouched];
        Assert.Equal(bill.Sum(s => s.Total), bill.Sum(s => s.AmountReceived) + bill.Sum(s => s.AmountDue));
    }

    /// <summary>
    /// A service settled before the AmountSettled column existed carries zero in it, so the flag
    /// has to stand in for the money. Without the fallback every historical payment would vanish
    /// from the bills the moment the rule started reading the column.
    /// </summary>
    [Fact]
    public void APaidServiceFromBeforeTheColumnCountsItsWholeTotal()
    {
        var legacy = Service(ServiceKind.Walk, 60m, paid: true, done: true);

        Assert.Equal(0m, legacy.AmountSettled);
        Assert.Equal(60m, legacy.AmountReceived);
    }

    /// <summary>
    /// Work not yet carried out is neither received nor chargeable, so it stays out of both
    /// columns — a booking is not money, however much it will be worth.
    /// </summary>
    [Fact]
    public void AnUnexecutedBookingIsNeitherReceivedNorDue()
    {
        var booking = Service(ServiceKind.Sitting, 90m);

        Assert.Equal(0m, booking.AmountReceived);
        Assert.Equal(0m, booking.AmountDue);
        Assert.Equal(90m, booking.AmountUpcoming);
    }
}