namespace PatasePasseios.Repository.Dapper.Dtos;

/// <summary>
/// What a settlement does to one service.
/// </summary>
/// <remarks>
/// A tutor pays a lump sum, or hands money over in advance that becomes credit; either way it is
/// spread over their services oldest first. Each service records how much of it has been settled
/// and keeps its price — earlier the odd one out had its price cut to the remainder, which
/// balanced but left no record of what the service actually cost.
/// </remarks>
/// <param name="kind">Which table the service lives in.</param>
/// <param name="serviceId">The service being settled.</param>
/// <param name="amount">How much this settlement puts against the service. Added to what is already settled.</param>
/// <param name="fullyPaid">Whether the service is fully settled once this is applied.</param>
/// <param name="fromCredit">
/// How much of <paramref name="amount"/> came out of the tutor's credit rather than a payment.
/// Recorded only so the service screen can say where the money came from.
/// </param>
public sealed class ServicePayment(ServiceKind kind, int serviceId, decimal amount, bool fullyPaid, decimal fromCredit = 0m)
{
    public ServiceKind Kind { get; } = kind;

    public int ServiceId { get; } = serviceId;

    /// <summary>Gets how much this settlement adds to the service's settled total.</summary>
    public decimal Amount { get; } = amount;

    public bool FullyPaid { get; } = fullyPaid;

    /// <summary>Gets the part of <see cref="Amount"/> funded by tutor credit.</summary>
    public decimal FromCredit { get; } = fromCredit;
}