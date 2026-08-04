using DapperDemo.Repository.Dapper.Dtos;

namespace DapperDemo.Repository.Dapper;

/// <summary>
/// How an amount received from a tutor is spread over what they owe.
/// </summary>
/// <remarks>
/// Alongside the repositories rather than in a view model because it is a money rule over
/// <see cref="ServiceItem"/> and <see cref="ServicePayment"/> and nothing else — no screen, no
/// formatting, no container. Static and free of state so it can be reasoned about and tested on
/// its own. <see cref="Aggregates.RepositoryServices.RegisterPaymentAsync"/> writes what this
/// decides.
/// </remarks>
public static class PaymentAllocation
{
    /// <summary>
    /// Spreads an amount over a tutor's chargeable services, oldest first.
    /// </summary>
    /// <remarks>
    /// Only executed work is chargeable, which <see cref="ServiceItem.AmountDue"/> already encodes,
    /// so a booking that has not been carried out is skipped however old it is. Services the money
    /// covers in full keep their price and are marked paid. The one service the money runs out on
    /// has its price cut to the remainder and stays unpaid — so a 100 service part-paid by 75
    /// becomes an unpaid 25. Anything left once every chargeable service is settled has nowhere to
    /// go here: the caller turns it into tutor credit rather than this deciding.
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="amount">The amount received.</param>
    /// <returns>The services to write, and how much of the payment was actually used.</returns>
    public static (List<ServicePayment> Payments, decimal Applied) Allocate(IEnumerable<ServiceItem> services, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(services);

        var payments = new List<ServicePayment>();
        var remaining = amount;

        // Oldest first, so the debt that has been outstanding longest is cleared first. ServiceId
        // breaks ties, which keeps the order stable for two services booked at the same moment.
        foreach (var service in services.Where(s => s.AmountDue > 0m).OrderBy(s => s.Date).ThenBy(s => s.ServiceId))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var due = service.AmountDue;
            if (remaining >= due)
            {
                remaining -= due;
                payments.Add(new ServicePayment(service.Kind, service.ServiceId, service.Price, true, service.ExtraCharge));
                continue;
            }

            // Falls short: the price becomes what is still owed. A hotel stay prices per night,
            // so the remainder is divided back out over the nights it spans.
            var shortfall = due - remaining;
            var newPrice = service.Kind == ServiceKind.Hotel
                ? decimal.Round(shortfall / service.Nights, 2, MidpointRounding.AwayFromZero)
                : shortfall;

            // The remainder is carried entirely by the rate, so any extra is folded in and the
            // extra column cleared — otherwise it would be charged on top a second time.
            payments.Add(new ServicePayment(service.Kind, service.ServiceId, newPrice, false, 0m));
            remaining = 0m;
            break;
        }

        return (payments, amount - remaining);
    }
}