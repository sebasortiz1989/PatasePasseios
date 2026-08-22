using Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Aggregates;

/// <summary>
/// Walks, pet sitting, hotel stays and day-care live in four tables but the app shows them as one
/// agenda, so this spans all four rather than extending <see cref="RepositoryBase{TEntity}"/> (which is
/// one-type-per-table). Reads come back as <see cref="ServiceItem"/>, already joined to the dog
/// and tutor names the screens display.
/// </summary>
public sealed class RepositoryServices(DapperDatabaseService dapperDatabaseService)
{
    /// <summary>
    /// The four tables are read with four separate queries rather than one UNION ALL, and then
    /// merged here. Microsoft.Data.Sqlite decides a column's CLR type from its *declared* type,
    /// which only a plain column reference carries — in a compound select the branches share the
    /// first branch's columns, so the hotel EndDate came back as a raw string that Dapper could
    /// not map to DateTime?. Querying each table on its own keeps every date a real DATETIME.
    /// </summary>
    private const string WalkSelect = """
        SELECT w.WalkingServiceId AS ServiceId, 0 AS Kind, w.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.TutorId, t.Name AS TutorName, t.Address AS TutorAddress, w.Date, w.Price, w.Discount, w.ServicePaid, w.ServiceDone, w.AmountSettled, w.CreditApplied
        FROM WalkingService w
        INNER JOIN Dogs d ON d.DogId = w.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE w.PetSitterId = @PetSitterId
        """;

    private const string SittingSelect = """
        SELECT s.PetSittingServiceId AS ServiceId, 1 AS Kind, s.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.TutorId, t.Name AS TutorName, t.Address AS TutorAddress, s.Date, s.Price, s.Discount, s.ServicePaid, s.ServiceDone, s.AmountSettled, s.CreditApplied
        FROM PetSittingService s
        INNER JOIN Dogs d ON d.DogId = s.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE s.PetSitterId = @PetSitterId
        """;

    private const string HotelSelect = """
        SELECT h.PetHotelServiceId AS ServiceId, 2 AS Kind, h.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.TutorId, t.Name AS TutorName, t.Address AS TutorAddress, h.StartDate AS Date, h.EndDate, h.PricePerDay AS Price, h.ExtraCharge, h.Discount, h.RequiresWalking, h.ServicePaid, h.ServiceDone, h.AmountSettled, h.CreditApplied
        FROM PetHotelService h
        INNER JOIN Dogs d ON d.DogId = h.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE h.PetSitterId = @PetSitterId
        """;

    private const string DayCareSelect = """
        SELECT c.DayCareServiceId AS ServiceId, 3 AS Kind, c.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.TutorId, t.Name AS TutorName, t.Address AS TutorAddress, c.Date, c.Price, c.Discount, c.RequiresWalking, c.ServicePaid, c.ServiceDone, c.AmountSettled, c.CreditApplied
        FROM DayCareService c
        INNER JOIN Dogs d ON d.DogId = c.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE c.PetSitterId = @PetSitterId
        """;

    private DapperDatabaseService DapperDatabaseService { get; } = dapperDatabaseService;

    /// <summary>Every service booked by this pet sitter, soonest first.</summary>
    public async Task<ServiceItem[]> ListForPetSitterAsync(int petSitterId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        var param = new { PetSitterId = petSitterId };

        var walks = await connection.QueryAsync<ServiceItem>(WalkSelect, param).ConfigureAwait(false);
        var sittings = await connection.QueryAsync<ServiceItem>(SittingSelect, param).ConfigureAwait(false);
        var hotels = await connection.QueryAsync<ServiceItem>(HotelSelect, param).ConfigureAwait(false);
        var dayCares = await connection.QueryAsync<ServiceItem>(DayCareSelect, param).ConfigureAwait(false);

        return [.. walks.Concat(sittings).Concat(hotels).Concat(dayCares).OrderBy(s => s.Date)];
    }

    /// <summary>The services booked for one dog, used by the dog and tutor detail screens.</summary>
    public async Task<ServiceItem[]> ListForDogAsync(int petSitterId, int dogId)
    {
        var all = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);
        return [.. all.Where(s => s.DogId == dogId)];
    }

    /// <summary>
    /// The services booked for every dog of one tutor — their whole history, which is what a bill
    /// and a payment are settled against.
    /// </summary>
    /// <param name="petSitterId">The signed-in account, which scopes the read.</param>
    /// <param name="tutorId">The tutor whose dogs' services to return.</param>
    /// <returns>The tutor's services, soonest first.</returns>
    public async Task<ServiceItem[]> ListForTutorAsync(int petSitterId, int tutorId)
    {
        var all = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);
        return [.. all.Where(s => s.TutorId == tutorId)];
    }

    public async Task<ServiceItem?> GetAsync(int petSitterId, ServiceKind kind, int serviceId)
    {
        var all = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);
        return all.FirstOrDefault(s => s.Kind == kind && s.ServiceId == serviceId);
    }

    public Task<Response> AddWalkAsync(WalkingService service) => InsertAsync(
        """
        INSERT INTO WalkingService (DogId, PetSitterId, Date, Price, Discount, ServicePaid, ServiceDone)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @Discount, @ServicePaid, @ServiceDone)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.Discount, service.ServicePaid, service.ServiceDone });

    public Task<Response> AddSittingAsync(PetSittingService service) => InsertAsync(
        """
        INSERT INTO PetSittingService (DogId, PetSitterId, Date, Price, Discount, ServicePaid, ServiceDone)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @Discount, @ServicePaid, @ServiceDone)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.Discount, service.ServicePaid, service.ServiceDone });

    public Task<Response> AddHotelAsync(PetHotelService service) => InsertAsync(
        """
        INSERT INTO PetHotelService (DogId, PetSitterId, StartDate, EndDate, PricePerDay, ExtraCharge, Discount, RequiresWalking, ServicePaid, ServiceDone)
        VALUES (@DogId, @PetSitterId, @StartDate, @EndDate, @PricePerDay, @ExtraCharge, @Discount, @RequiresWalking, @ServicePaid, @ServiceDone)
        """,
        new { service.DogId, service.PetSitterId, service.StartDate, service.EndDate, service.PricePerDay, service.ExtraCharge, service.Discount, service.RequiresWalking, service.ServicePaid, service.ServiceDone });

    public Task<Response> AddDayCareAsync(DayCareService service) => InsertAsync(
        """
        INSERT INTO DayCareService (DogId, PetSitterId, Date, Price, Discount, RequiresWalking, ServicePaid, ServiceDone)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @Discount, @RequiresWalking, @ServicePaid, @ServiceDone)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.Discount, service.RequiresWalking, service.ServicePaid, service.ServiceDone });

    /// <summary>
    /// Saves an edit to an existing booking: its date, its price, its discount, and for a hotel
    /// stay the check-out date and whether walks are included.
    /// </summary>
    /// <remarks>
    /// The dog and the kind are not editable. Each kind lives in its own table, so changing one
    /// would mean deleting the row and inserting into another — a different operation from an
    /// edit, and one that would silently break anything holding the old id.
    /// </remarks>
    /// <param name="service">The booking, carrying its new values. Kind and ServiceId locate the row.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> UpdateAsync(ServiceItem service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var (sql, param) = service.Kind switch
        {
            ServiceKind.Walk => (
                "UPDATE WalkingService SET Date = @Date, Price = @Price, Discount = @Discount WHERE WalkingServiceId = @ServiceId",
                (object)new { service.Date, service.Price, service.Discount, service.ServiceId }),
            ServiceKind.Sitting => (
                "UPDATE PetSittingService SET Date = @Date, Price = @Price, Discount = @Discount WHERE PetSittingServiceId = @ServiceId",
                new { service.Date, service.Price, service.Discount, service.ServiceId }),
            ServiceKind.Hotel => (
                """
                UPDATE PetHotelService
                SET StartDate = @Date, EndDate = @EndDate, PricePerDay = @Price, ExtraCharge = @ExtraCharge, Discount = @Discount, RequiresWalking = @RequiresWalking
                WHERE PetHotelServiceId = @ServiceId
                """,
                new { service.Date, service.EndDate, service.Price, service.ExtraCharge, service.Discount, service.RequiresWalking, service.ServiceId }),

            // Date is normalised to midnight: day-care has no time of day, so an edit must not
            // let one back in through the date picker.
            ServiceKind.DayCare => (
                """
                UPDATE DayCareService
                SET Date = @Date, Price = @Price, Discount = @Discount, RequiresWalking = @RequiresWalking
                WHERE DayCareServiceId = @ServiceId
                """,
                new { Date = service.Date.Date, service.Price, service.Discount, service.RequiresWalking, service.ServiceId }),
            _ => throw new ArgumentOutOfRangeException(nameof(service)),
        };

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(sql, param).ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>Removes a single booking, whichever of the four tables it lives in.</summary>
    public async Task<Response> DeleteAsync(ServiceKind kind, int serviceId)
    {
        var (table, key) = ServiceTables.For(kind);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);

            // The payment ledger's allocations go with it: a reversal must not try to unsettle a
            // row that is no longer there. The payment headers stay — the tutor did hand that
            // money over, whatever became of the booking it paid for.
            await connection.ExecuteAsync(
                sql: $"""
                      DELETE FROM {table} WHERE {key} = @ServiceId;
                      DELETE FROM TutorPaymentAllocations WHERE Kind = @Kind AND ServiceId = @ServiceId;
                      """,
                param: new { ServiceId = serviceId, Kind = (int)kind }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>Marks a service paid or pending. The table and key column depend on the kind.</summary>
    public async Task<Response> SetPaidAsync(ServiceKind kind, int serviceId, bool paid)
    {
        var (table, key) = ServiceTables.For(kind);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: $"UPDATE {table} SET ServicePaid = @Paid WHERE {key} = @ServiceId",
                param: new { Paid = paid, ServiceId = serviceId }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Marks a service done or still to do. The table and key column depend on the kind.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="SetPaidAsync"/> on purpose: settling a booking says nothing about
    /// whether it happened, and a walk can be done long before the tutor pays for it.
    /// </remarks>
    /// <param name="kind">Which of the four service tables the booking lives in.</param>
    /// <param name="serviceId">The booking's id within that table.</param>
    /// <param name="done">Whether the work has been carried out.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> SetDoneAsync(ServiceKind kind, int serviceId, bool done)
    {
        var (table, key) = ServiceTables.For(kind);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: $"UPDATE {table} SET ServiceDone = @Done WHERE {key} = @ServiceId",
                param: new { Done = done, ServiceId = serviceId }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Writes a payment that has already been split across services, all or nothing.
    /// </summary>
    /// <remarks>
    /// One transaction because a payment is one event: a crash halfway through must not leave a
    /// tutor credited for part of what they handed over. The split is worked out by the caller —
    /// see <see cref="ServicePayment"/>. A service the payment only partly covers has its price
    /// cut to what is still owed and stays unpaid, so the balance is carried by the price itself
    /// rather than by a second column.
    /// </remarks>
    /// <param name="payments">The services to settle, in the order the money reaches them.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> RegisterPaymentAsync(IReadOnlyList<ServicePayment> payments)
    {
        ArgumentNullException.ThrowIfNull(payments);

        if (payments.Count == 0)
        {
            return Response.Successful;
        }

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            foreach (var payment in payments)
            {
                var (table, key) = ServiceTables.For(payment.Kind);

                // Accumulated, not assigned: a service can be settled more than once — some credit
                // at booking, cash later — and each write must add to what is already there. The
                // price is deliberately untouched, so the record of what the service cost survives.
                await connection.ExecuteAsync(
                    sql: $"""
                          UPDATE {table}
                          SET AmountSettled = AmountSettled + @Amount,
                              CreditApplied = CreditApplied + @FromCredit,
                              ServicePaid = @FullyPaid
                          WHERE {key} = @ServiceId
                          """,
                    param: new { payment.Amount, payment.FromCredit, payment.FullyPaid, payment.ServiceId },
                    transaction: transaction).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    public async Task<MonthlyIncome> GetMonthlyIncomeAsync(int petSitterId, int year, int month)
    {
        var services = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);

        // Settled, not "paid": a service can now be part-settled, and what came in is that partial
        // amount rather than nothing. A fully paid service settled before this column existed has
        // AmountSettled 0, so ServicePaid falls back to its full total.
        // BillingDate, not Date: a stay finishing in August is August's money even though it
        // started in July. See ServiceItem.BillingDate.
        var thisMonth = services
            .Where(s => s.BillingDate.Year == year && (month <= 0 || s.BillingDate.Month == month))
            .ToArray();

        decimal Received(ServiceKind kind) => thisMonth
            .Where(s => s.Kind == kind)
            .Sum(s => s.ServicePaid ? Math.Max(s.AmountSettled, s.Total) : s.AmountSettled);

        return new MonthlyIncome
        {
            Walk = Received(ServiceKind.Walk),
            Sitting = Received(ServiceKind.Sitting),
            Hotel = Received(ServiceKind.Hotel),
            DayCare = Received(ServiceKind.DayCare),
        };
    }

    private async Task<Response> InsertAsync(string sql, object param)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(sql, param).ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }
}