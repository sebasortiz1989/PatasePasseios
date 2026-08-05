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
    /// Spreads an amount received over a tutor's unsettled services, oldest first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Money in hand settles whatever is outstanding, whether or not the work has been carried out
    /// yet. The executed-before-paid rule governs what the sitter may <em>ask</em> for — which is
    /// what <see cref="ServiceItem.AmountDue"/> encodes and what the tutor's balance shows — not
    /// what a payment already received is allowed to clear.
    /// </para>
    /// <para>
    /// It used to skip unexecuted bookings, which made a payment behave differently from the credit
    /// that same payment turned into moments later: pay before anything was ticked done and the
    /// whole amount fell through to credit, then ticking one service done spent that credit across
    /// every booking at once. Same money, two rules.
    /// </para>
    /// <para>
    /// Every service keeps its price; what the money covers is recorded against it, so a 100 service
    /// part-paid by 75 stays a 100 service with 75 settled. Anything left once everything is settled
    /// has nowhere to go here — the caller turns it into tutor credit rather than this deciding.
    /// </para>
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="amount">The amount received.</param>
    /// <returns>The settlements to write, and how much of the payment was actually used.</returns>
    public static (List<ServicePayment> Payments, decimal Applied) Allocate(IEnumerable<ServiceItem> services, decimal amount) =>
        Allocate(services, amount, fromCredit: false);

    /// <summary>
    /// Spreads a tutor's credit over their services, oldest first.
    /// </summary>
    /// <remarks>
    /// Identical to <see cref="Allocate(IEnumerable{ServiceItem}, decimal)"/> except that what it
    /// settles is attributed to credit, so the service screen can say where the money came from.
    /// </remarks>
    /// <param name="services">The tutor's services, any order.</param>
    /// <param name="credit">The credit available to spend.</param>
    /// <returns>The settlements to write, and how much credit was actually used.</returns>
    public static (List<ServicePayment> Payments, decimal Applied) AllocateCredit(IEnumerable<ServiceItem> services, decimal credit) =>
        Allocate(services, credit, fromCredit: true);

    private static (List<ServicePayment> Payments, decimal Applied) Allocate(
        IEnumerable<ServiceItem> services,
        decimal amount,
        bool fromCredit)
    {
        static decimal Eligible(ServiceItem service) => service.ServicePaid ? 0m : service.Outstanding;

        ArgumentNullException.ThrowIfNull(services);

        var payments = new List<ServicePayment>();
        var remaining = amount;

        // Oldest first, so the debt that has been outstanding longest is cleared first. ServiceId
        // breaks ties, which keeps the order stable for two services booked at the same moment.
        foreach (var service in services.Where(s => Eligible(s) > 0m).OrderBy(s => s.Date).ThenBy(s => s.ServiceId))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var due = Eligible(service);
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