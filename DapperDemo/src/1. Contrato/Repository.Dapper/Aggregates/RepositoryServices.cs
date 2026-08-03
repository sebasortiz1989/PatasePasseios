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
               t.Name AS TutorName, t.Address AS TutorAddress, w.Date, w.Price, w.ServicePaid
        FROM WalkingService w
        INNER JOIN Dogs d ON d.DogId = w.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE w.PetSitterId = @PetSitterId
        """;

    private const string SittingSelect = """
        SELECT s.PetSittingServiceId AS ServiceId, 1 AS Kind, s.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.Name AS TutorName, t.Address AS TutorAddress, s.Date, s.Price, s.ServicePaid
        FROM PetSittingService s
        INNER JOIN Dogs d ON d.DogId = s.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE s.PetSitterId = @PetSitterId
        """;

    private const string HotelSelect = """
        SELECT h.PetHotelServiceId AS ServiceId, 2 AS Kind, h.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.Name AS TutorName, t.Address AS TutorAddress, h.StartDate AS Date, h.EndDate, h.PricePerDay AS Price, h.RequiresWalking, h.ServicePaid
        FROM PetHotelService h
        INNER JOIN Dogs d ON d.DogId = h.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE h.PetSitterId = @PetSitterId
        """;

    private const string DayCareSelect = """
        SELECT c.DayCareServiceId AS ServiceId, 3 AS Kind, c.DogId, d.Name AS DogName, d.Image AS DogImage,
               t.Name AS TutorName, t.Address AS TutorAddress, c.Date, c.Price, c.RequiresWalking, c.ServicePaid
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

    public async Task<ServiceItem?> GetAsync(int petSitterId, ServiceKind kind, int serviceId)
    {
        var all = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);
        return all.FirstOrDefault(s => s.Kind == kind && s.ServiceId == serviceId);
    }

    public Task<Response> AddWalkAsync(WalkingService service) => InsertAsync(
        """
        INSERT INTO WalkingService (DogId, PetSitterId, Date, Price, ServicePaid)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @ServicePaid)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.ServicePaid });

    public Task<Response> AddSittingAsync(PetSittingService service) => InsertAsync(
        """
        INSERT INTO PetSittingService (DogId, PetSitterId, Date, Price, ServicePaid)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @ServicePaid)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.ServicePaid });

    public Task<Response> AddHotelAsync(PetHotelService service) => InsertAsync(
        """
        INSERT INTO PetHotelService (DogId, PetSitterId, StartDate, EndDate, PricePerDay, RequiresWalking, ServicePaid)
        VALUES (@DogId, @PetSitterId, @StartDate, @EndDate, @PricePerDay, @RequiresWalking, @ServicePaid)
        """,
        new { service.DogId, service.PetSitterId, service.StartDate, service.EndDate, service.PricePerDay, service.RequiresWalking, service.ServicePaid });

    public Task<Response> AddDayCareAsync(DayCareService service) => InsertAsync(
        """
        INSERT INTO DayCareService (DogId, PetSitterId, Date, Price, RequiresWalking, ServicePaid)
        VALUES (@DogId, @PetSitterId, @Date, @Price, @RequiresWalking, @ServicePaid)
        """,
        new { service.DogId, service.PetSitterId, service.Date, service.Price, service.RequiresWalking, service.ServicePaid });

    /// <summary>
    /// Saves an edit to an existing booking: its date, its price, and for a hotel stay the
    /// check-out date and whether walks are included.
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
                "UPDATE WalkingService SET Date = @Date, Price = @Price WHERE WalkingServiceId = @ServiceId",
                (object)new { service.Date, service.Price, service.ServiceId }),
            ServiceKind.Sitting => (
                "UPDATE PetSittingService SET Date = @Date, Price = @Price WHERE PetSittingServiceId = @ServiceId",
                new { service.Date, service.Price, service.ServiceId }),
            ServiceKind.Hotel => (
                """
                UPDATE PetHotelService
                SET StartDate = @Date, EndDate = @EndDate, PricePerDay = @Price, RequiresWalking = @RequiresWalking
                WHERE PetHotelServiceId = @ServiceId
                """,
                new { service.Date, service.EndDate, service.Price, service.RequiresWalking, service.ServiceId }),

            // Date is normalised to midnight: day-care has no time of day, so an edit must not
            // let one back in through the date picker.
            ServiceKind.DayCare => (
                """
                UPDATE DayCareService
                SET Date = @Date, Price = @Price, RequiresWalking = @RequiresWalking
                WHERE DayCareServiceId = @ServiceId
                """,
                new { Date = service.Date.Date, service.Price, service.RequiresWalking, service.ServiceId }),
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
        var (table, key) = TableFor(kind);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: $"DELETE FROM {table} WHERE {key} = @ServiceId",
                param: new { ServiceId = serviceId }).ConfigureAwait(false);

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
        var (table, key) = TableFor(kind);

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
    /// Billing for one month, counting only services already marked paid. Hotel stays contribute
    /// their daily rate, matching how the price is entered and shown.
    /// </summary>
    public async Task<MonthlyIncome> GetMonthlyIncomeAsync(int petSitterId, int year, int month)
    {
        var services = await ListForPetSitterAsync(petSitterId).ConfigureAwait(false);
        var paidThisMonth = services
            .Where(s => s.ServicePaid && s.Date.Year == year && s.Date.Month == month)
            .ToArray();

        return new MonthlyIncome
        {
            Walk = paidThisMonth.Where(s => s.Kind == ServiceKind.Walk).Sum(s => s.Price),
            Sitting = paidThisMonth.Where(s => s.Kind == ServiceKind.Sitting).Sum(s => s.Price),
            Hotel = paidThisMonth.Where(s => s.Kind == ServiceKind.Hotel).Sum(s => s.Price),
            DayCare = paidThisMonth.Where(s => s.Kind == ServiceKind.DayCare).Sum(s => s.Price),
        };
    }

    private static (string Table, string Key) TableFor(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => ("WalkingService", "WalkingServiceId"),
        ServiceKind.Sitting => ("PetSittingService", "PetSittingServiceId"),
        ServiceKind.Hotel => ("PetHotelService", "PetHotelServiceId"),
        ServiceKind.DayCare => ("DayCareService", "DayCareServiceId"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

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