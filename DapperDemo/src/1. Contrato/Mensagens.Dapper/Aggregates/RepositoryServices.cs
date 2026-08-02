using Dapper;
using Microsoft.Data.Sqlite;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Mensagens.Dapper.Services;

namespace DapperDemo.Mensagens.Dapper.Aggregates;

/// <summary>
/// Walks, pet sitting and hotel stays live in three tables but the app shows them as one agenda,
/// so this spans all three rather than extending <see cref="RepositoryBase{TEntity}"/> (which is
/// one-type-per-table). Reads come back as <see cref="ServiceItem"/>, already joined to the dog
/// and tutor names the screens display.
/// </summary>
public sealed class RepositoryServices(DapperDatabaseService dapperDatabaseService)
{
    /// <summary>
    /// The three tables are read with three separate queries rather than one UNION ALL, and then
    /// merged here. Microsoft.Data.Sqlite decides a column's CLR type from its *declared* type,
    /// which only a plain column reference carries — in a compound select the branches share the
    /// first branch's columns, so the hotel EndDate came back as a raw string that Dapper could
    /// not map to DateTime?. Querying each table on its own keeps every date a real DATETIME.
    /// </summary>
    private const string WalkSelect = """
        SELECT w.WalkingServiceId AS ServiceId, 0 AS Kind, w.DogId, d.Name AS DogName, t.Name AS TutorName,
               w.Date, w.Price, w.ServicePaid
        FROM WalkingService w
        INNER JOIN Dogs d ON d.DogId = w.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE w.PetSitterId = @PetSitterId
        """;

    private const string SittingSelect = """
        SELECT s.PetSittingServiceId AS ServiceId, 1 AS Kind, s.DogId, d.Name AS DogName, t.Name AS TutorName,
               s.Date, s.Price, s.ServicePaid
        FROM PetSittingService s
        INNER JOIN Dogs d ON d.DogId = s.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE s.PetSitterId = @PetSitterId
        """;

    private const string HotelSelect = """
        SELECT h.PetHotelServiceId AS ServiceId, 2 AS Kind, h.DogId, d.Name AS DogName, t.Name AS TutorName,
               h.StartDate AS Date, h.EndDate, h.PricePerDay AS Price, h.RequiresWalking, h.ServicePaid
        FROM PetHotelService h
        INNER JOIN Dogs d ON d.DogId = h.DogId
        INNER JOIN Tutors t ON t.TutorId = d.TutorId
        WHERE h.PetSitterId = @PetSitterId
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

        return [.. walks.Concat(sittings).Concat(hotels).OrderBy(s => s.Date)];
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

    /// <summary>Removes a single booking, whichever of the three tables it lives in.</summary>
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
            Hotel = paidThisMonth.Where(s => s.Kind == ServiceKind.Hotel).Sum(s => s.Price)
        };
    }

    private static (string Table, string Key) TableFor(ServiceKind kind) => kind switch
    {
        ServiceKind.Walk => ("WalkingService", "WalkingServiceId"),
        ServiceKind.Sitting => ("PetSittingService", "PetSittingServiceId"),
        ServiceKind.Hotel => ("PetHotelService", "PetHotelServiceId"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
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
