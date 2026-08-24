namespace PatasePasseios.Repository.Dapper.Dtos;

/// <summary>
/// One amount a tutor handed over, kept as a record of its own.
/// </summary>
/// <remarks>
/// <para>
/// Until this table existed a payment left no trace: it was spread over the services it settled
/// and, if anything was left, banked as credit, and nothing said the three had been one event.
/// A sitter who typed 500 instead of 50 had no way back — the money was already scattered across
/// service rows. This is what makes a payment correctable: the header says what was received, the
/// allocations say where it went, and together they can be unwound.
/// </para>
/// <para>
/// Only money the tutor actually handed over is recorded here. Credit being spent against a
/// booking is a consequence of an earlier payment, not a payment of its own, so credit spending
/// stays out of the ledger.
/// </para>
/// </remarks>
public sealed class TutorPayment
{
    public int TutorPaymentId { get; init; }

    public int TutorId { get; init; }

    /// <summary>Gets the account that took the payment. Scopes the service read a reversal needs.</summary>
    public int PetSitterId { get; init; }

    /// <summary>Gets when the money was received. An edit keeps the original date.</summary>
    public DateTime Date { get; init; }

    /// <summary>Gets the full amount received, whatever it ended up covering.</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the part of <see cref="Amount"/> that had no chargeable work to land on and was banked
    /// as tutor credit. Reversing the payment takes it back, following it into the services it was
    /// later spent on if it has already been used.
    /// </summary>
    public decimal CreditStored { get; init; }

    /// <summary>
    /// Gets where the rest of <see cref="Amount"/> went, one entry per service it settled.
    /// </summary>
    /// <remarks>
    /// Stored as <see cref="ServicePayment"/> because that is what
    /// <see cref="PaymentAllocation.Allocate(IEnumerable{ServiceItem}, decimal)"/> produces and what
    /// the write consumes. Only the kind, the service and the amount are persisted: a reversal
    /// recomputes whether the service is still fully paid from its own totals rather than trusting
    /// a flag written when the payment was taken, and cash payments never draw on credit.
    /// </remarks>
    public IReadOnlyList<ServicePayment> Allocations { get; init; } = [];
}

/*
CREATE TABLE IF NOT EXISTS TutorPayments (
    TutorPaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
    TutorId INTEGER NOT NULL,
    PetSitterId INTEGER NOT NULL,
    Date DATETIME NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    CreditStored DECIMAL(10, 2) NOT NULL DEFAULT 0,
    FOREIGN KEY (TutorId) REFERENCES Tutors(TutorId),
    FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));

CREATE TABLE IF NOT EXISTS TutorPaymentAllocations (
    TutorPaymentAllocationId INTEGER PRIMARY KEY AUTOINCREMENT,
    TutorPaymentId INTEGER NOT NULL,
    Kind INTEGER NOT NULL,
    ServiceId INTEGER NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (TutorPaymentId) REFERENCES TutorPayments(TutorPaymentId));
*/