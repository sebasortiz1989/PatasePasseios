namespace DapperDemo.Repository.Dapper.Dtos;

/// <summary>
/// What a payment does to one service.
/// </summary>
/// <remarks>
/// A tutor pays a lump sum, which is then spread over what they owe, oldest first. Services the
/// money covers in full are simply marked paid at their existing price. At most one service is
/// left part-covered: its price is cut to the remainder and it stays unpaid, so the outstanding
/// balance lives in the price rather than in a separate column.
/// </remarks>
/// <param name="kind">Which table the service lives in.</param>
/// <param name="serviceId">The service being settled.</param>
/// <param name="price">
/// The price to store. Unchanged for a service paid in full; for a hotel stay it is a nightly
/// rate, so a reduced figure is the remainder divided by the nights.
/// </param>
/// <param name="fullyPaid">Whether this payment clears the service.</param>
public sealed class ServicePayment(ServiceKind kind, int serviceId, decimal price, bool fullyPaid)
{
    public ServiceKind Kind { get; } = kind;

    public int ServiceId { get; } = serviceId;

    /// <summary>Gets the price to store — reduced only when the payment falls short.</summary>
    public decimal Price { get; } = price;

    public bool FullyPaid { get; } = fullyPaid;
}