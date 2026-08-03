namespace DapperDemo.Repository.Dapper.Dtos;

/// <summary>
/// The four service tables are stored separately but the app presents them as one agenda,
/// so reads come back as this shared shape rather than as four unrelated row types.
/// </summary>
/// <remarks>
/// The numbers are baked into the agenda queries as literals (<c>0 AS Kind</c> and so on), not
/// stored in any table, so new kinds must be appended rather than inserted.
/// </remarks>
public enum ServiceKind
{
    Walk,
    Sitting,
    Hotel,

    /// <summary>A single day at the sitter's, with no check-out and no time of day.</summary>
    DayCare,
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

    /// <summary>Gets the dog's photo file name, or null when it has none. See DogImageStore.</summary>
    public string? DogImage { get; init; }

    public required string TutorName { get; init; }

    /// <summary>
    /// Gets where the tutor lives, or null when they have none recorded. Carried on the agenda row
    /// because every service query already joins Tutors — a screen needing it should not have to
    /// go back to the database for one column.
    /// </summary>
    public string? TutorAddress { get; init; }

    public DateTime Date { get; init; }

    /// <summary>Gets check-out date; only hotel stays have one.</summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Gets a one-off fee for walks, pet sitting and day-care; a daily rate for hotel stays.
    /// Day-care covers a single day, so its fee needs no multiplying.
    /// </summary>
    public decimal Price { get; init; }

    public bool RequiresWalking { get; init; }

    public bool ServicePaid { get; init; }
}