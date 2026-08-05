using DapperDemo.Repository.Dapper.Dtos;

namespace DapperDemo.Repository.Dapper.Aggregates;

/// <summary>
/// Which table and key column each <see cref="ServiceKind"/> lives in.
/// </summary>
/// <remarks>
/// Shared rather than private to <see cref="RepositoryServices"/> because
/// <see cref="RepositoryPayments"/> writes to the same four tables when it settles or unwinds a
/// payment. Two copies of this map is exactly how a fifth service kind ends up half-supported.
/// </remarks>
internal static class ServiceTables
{
    public static (string Table, string Key) For(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => ("WalkingService", "WalkingServiceId"),
        ServiceKind.Sitting => ("PetSittingService", "PetSittingServiceId"),
        ServiceKind.Hotel => ("PetHotelService", "PetHotelServiceId"),
        ServiceKind.DayCare => ("DayCareService", "DayCareServiceId"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}