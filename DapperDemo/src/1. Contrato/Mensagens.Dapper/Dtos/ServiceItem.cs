namespace Verion.Treinamento.Mensagens.Dapper.Dtos;

/// <summary>
/// The three service tables are stored separately but the app presents them as one agenda,
/// so reads come back as this shared shape rather than as three unrelated row types.
/// </summary>
public enum ServiceKind
{
    Walk,
    Sitting,
    Hotel
}

/// <summary>
/// One row of the unified agenda: a service of any kind, already joined to its dog and tutor
/// so the list and detail screens don't have to look names up per row.
/// </summary>
public sealed class ServiceItem
{
    public int ServiceId { get; init; }

    public ServiceKind Kind { get; init; }

    public int DogId { get; init; }

    public required string DogName { get; init; }

    public required string TutorName { get; init; }

    public DateTime Date { get; init; }

    /// <summary>Check-out date; only hotel stays have one.</summary>
    public DateTime? EndDate { get; init; }

    /// <summary>A one-off fee for walks and pet sitting; a daily rate for hotel stays.</summary>
    public decimal Price { get; init; }

    public bool RequiresWalking { get; init; }

    public bool ServicePaid { get; init; }
}

/// <summary>Billing totals for one month, split the way the Perfil screen shows them.</summary>
public sealed class MonthlyIncome
{
    public decimal Walk { get; init; }

    public decimal Sitting { get; init; }

    public decimal Hotel { get; init; }

    public decimal Total => Walk + Sitting + Hotel;
}
