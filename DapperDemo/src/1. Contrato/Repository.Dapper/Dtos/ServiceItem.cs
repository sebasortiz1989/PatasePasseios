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

    /// <summary>
    /// Gets a one-off amount added on top of a hotel stay, such as a late pick-up. Zero for every
    /// other kind.
    /// </summary>
    public decimal ExtraCharge { get; init; }

    public bool RequiresWalking { get; init; }

    public bool ServicePaid { get; init; }

    /// <summary>
    /// Gets a value indicating whether the booking actually happened.
    /// </summary>
    /// <remarks>
    /// Deliberately independent of <see cref="ServicePaid"/> and of <see cref="Date"/>: work is
    /// often done before it is settled and sometimes settled before it is done, and a date in the
    /// past is no proof the sitter turned up. Only the sitter marking it makes it true.
    /// </remarks>
    public bool ServiceDone { get; init; }

    /// <summary>
    /// Gets the nights billed. Only a hotel stay spans more than one; check-in and check-out on
    /// the same day still bills one, which is how a day rate is charged and stops a stay totalling
    /// nothing.
    /// </summary>
    public int Nights => EndDate is DateTime finish ? Math.Max((finish.Date - Date.Date).Days, 1) : 1;

    /// <summary>
    /// Gets what this service costs in full. A hotel stay's <see cref="Price"/> is a nightly rate
    /// so it multiplies out; everything else is a one-off fee.
    /// </summary>
    public decimal Total => Kind == ServiceKind.Hotel ? (Price * Nights) + ExtraCharge : Price;

    /// <summary>
    /// Gets what may be charged for this service right now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Work is only billable once it has happened, so an unexecuted booking is worth nothing yet
    /// however much it will eventually cost — see <see cref="AmountUpcoming"/> for that figure.
    /// This is the single place that rule lives; everything that totals a balance goes through
    /// here rather than filtering on <see cref="ServicePaid"/> itself.
    /// </para>
    /// <para>
    /// There is no separate "amount paid" column: a part-paid service has its price cut to the
    /// remainder and stays unpaid, so what is owed is simply the whole of an unpaid service.
    /// </para>
    /// </remarks>
    public decimal AmountDue => ServicePaid || !ServiceDone ? 0m : Total;

    /// <summary>
    /// Gets what this booking will be worth once it has been carried out, and nothing once it has.
    /// Money the sitter has coming rather than money they may ask for today.
    /// </summary>
    public decimal AmountUpcoming => ServicePaid || ServiceDone ? 0m : Total;
}