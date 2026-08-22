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

    /// <summary>
    /// Gets the tutor who owns the dog. Carried on the row because every service query already
    /// joins Tutors, and both the bill and a payment reversal work per tutor rather than per dog.
    /// </summary>
    public int TutorId { get; init; }

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

    /// <summary>
    /// Gets the percentage taken off this booking, 0 to 100. Zero means full price.
    /// </summary>
    /// <remarks>
    /// A rate rather than an amount, so it keeps meaning the same thing when the price is edited
    /// afterwards — a stay discounted "10%" stays 10% off whatever it now costs, where a stored
    /// amount would quietly become a different fraction of the bill.
    /// </remarks>
    public decimal Discount { get; init; }

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
    /// Gets the date this service's money belongs to.
    /// </summary>
    /// <remarks>
    /// The check-out for a stay, the date itself for everything else. A stay that runs 29 July to
    /// 2 August is one piece of work that finishes in August, and it is billed once — so all of it
    /// counts as August's money rather than being split across two months or landing in July
    /// because that is when the dog arrived.
    /// <para>
    /// This is for <b>attributing money to a period</b> — the monthly income, the per-dog
    /// breakdown, the tutor's bill by month. The agenda keeps using <see cref="Date"/>, because a
    /// stay starting on the 29th is something the sitter has to turn up for on the 29th.
    /// </para>
    /// </remarks>
    public DateTime BillingDate => EndDate ?? Date;

    /// <summary>
    /// Gets what this service costs before any discount. A hotel stay's <see cref="Price"/> is a
    /// nightly rate so it multiplies out; everything else is a one-off fee.
    /// </summary>
    public decimal Subtotal => Kind == ServiceKind.Hotel ? (Price * Nights) + ExtraCharge : Price;

    /// <summary>
    /// Gets how much <see cref="Discount"/> takes off, in money.
    /// </summary>
    /// <remarks>
    /// Rounded to centavos here rather than left to trickle through the rest of the arithmetic,
    /// so what the tutor is shown on the service screen is exactly what the bill, the payment
    /// allocation and the report all work from. The rate is clamped because nothing stops a
    /// hand-edited database holding 150 or -5, and either would make the total nonsense.
    /// </remarks>
    public decimal DiscountAmount => Math.Round(
        Subtotal * (Math.Clamp(Discount, 0m, 100m) / 100m),
        2,
        MidpointRounding.AwayFromZero);

    /// <summary>
    /// Gets what this service costs in full, with any discount already taken off.
    /// </summary>
    /// <remarks>
    /// The discount lands here rather than anywhere further down because this is what every
    /// balance, allocation and report is built from — <see cref="Outstanding"/>,
    /// <see cref="AmountDue"/>, the tutor's bill and the monthly income all read through it, so a
    /// discounted booking is discounted everywhere without any of them knowing the rate exists.
    /// </remarks>
    public decimal Total => Subtotal - DiscountAmount;

    /// <summary>
    /// Gets how much of <see cref="Total"/> has already been settled, by cash or by credit.
    /// </summary>
    /// <remarks>
    /// A part-settled service keeps its price and records what has been paid against it. Earlier
    /// this cut the price down to the remainder instead, which balanced but destroyed the record
    /// of what the service actually cost.
    /// </remarks>
    public decimal AmountSettled { get; init; }

    /// <summary>
    /// Gets the part of <see cref="AmountSettled"/> that came out of the tutor's credit rather than
    /// a payment. Kept separately only so the screen can say where the money came from.
    /// </summary>
    public decimal CreditApplied { get; init; }

    /// <summary>Gets what is still unsettled, whether or not it may be charged yet.</summary>
    public decimal Outstanding => Math.Max(Total - AmountSettled, 0m);

    /// <summary>
    /// Gets what may be charged for this service right now.
    /// </summary>
    /// <remarks>
    /// Work is only billable once it has happened, so an unexecuted booking is worth nothing yet
    /// however much it will eventually cost — see <see cref="AmountUpcoming"/> for that figure.
    /// This is the single place that rule lives; everything that totals a balance goes through
    /// here rather than filtering on <see cref="ServicePaid"/> itself.
    /// </remarks>
    public decimal AmountDue => ServicePaid || !ServiceDone ? 0m : Outstanding;

    /// <summary>
    /// Gets what this booking will be worth once it has been carried out, and nothing once it has.
    /// Money the sitter has coming rather than money they may ask for today.
    /// </summary>
    public decimal AmountUpcoming => ServicePaid || ServiceDone ? 0m : Outstanding;
}