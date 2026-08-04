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
    /// so a booking that has not been carried out is skipped however old it is. Every service keeps
    /// its price; what the money covers is recorded against it instead, so a 100 service part-paid
    /// by 75 stays a 100 service with 75 settled. Anything left once every chargeable service is
    /// settled has nowhere to go here: the caller turns it into tutor credit rather than this
    /// deciding.
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="amount">The amount received.</param>
    /// <returns>The settlements to write, and how much of the payment was actually used.</returns>
    public static (List<ServicePayment> Payments, decimal Applied) Allocate(IEnumerable<ServiceItem> services, decimal amount) =>
        Allocate(services, amount, s => s.AmountDue, fromCredit: false);

    /// <summary>
    /// Spreads a tutor's credit over their services, including ones not yet carried out.
    /// </summary>
    /// <remarks>
    /// The difference from <see cref="Allocate(IEnumerable{ServiceItem}, decimal)"/> is which
    /// services are eligible. Credit is money the tutor has already handed over, so applying it to
    /// a booking that has not happened yet is bookkeeping rather than billing — the
    /// executed-before-paid rule exists to stop the sitter *asking* for money too early, not to
    /// stop them recording money they already hold.
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="credit">The credit available to spend.</param>
    /// <returns>The settlements to write, and how much credit was actually used.</returns>
    public static (List<ServicePayment> Payments, decimal Applied) AllocateCredit(IEnumerable<ServiceItem> services, decimal credit) =>
        Allocate(services, credit, s => s.ServicePaid ? 0m : s.Outstanding, fromCredit: true);

    private static (List<ServicePayment> Payments, decimal Applied) Allocate(
        IEnumerable<ServiceItem> services,
        decimal amount,
        Func<ServiceItem, decimal> eligible,
        bool fromCredit)
    {
        ArgumentNullException.ThrowIfNull(services);

        var payments = new List<ServicePayment>();
        var remaining = amount;

        // Oldest first, so the debt that has been outstanding longest is cleared first. ServiceId
        // breaks ties, which keeps the order stable for two services booked at the same moment.
        foreach (var service in services.Where(s => eligible(s) > 0m).OrderBy(s => s.Date).ThenBy(s => s.ServiceId))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var due = eligible(service);
            var applied = Math.Min(remaining, due);
            remaining -= applied;

            payments.Add(new ServicePayment(
                service.Kind,
                service.ServiceId,
                applied,
                fullyPaid: applied >= due,
                fromCredit ? applied : 0m));
        }

        return (payments, amount - remaining);
    }
}