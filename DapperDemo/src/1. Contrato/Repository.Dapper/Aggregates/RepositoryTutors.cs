using Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Aggregates;

/// <summary>
/// Tutors always belong to the pet sitter who created them (via the PetSitterTutors join table),
/// so the useful entry points here are the scoped ones below rather than the unscoped members
/// inherited from <see cref="RepositoryBase{TEntity}"/>.
/// </summary>
public sealed class RepositoryTutors(DapperDatabaseService dapperDatabaseService) : RepositoryBase<Tutors>(dapperDatabaseService)
{
    /// <summary>
    /// Inserts the tutor and links it to the pet sitter in one transaction, so a tutor is never
    /// left orphaned and invisible to the account that created it.
    /// </summary>
    public async Task<Response> AddForPetSitterAsync(Tutors tutor, int petSitterId)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            var tutorId = await connection.ExecuteScalarAsync<int>(
                sql: """
                     INSERT INTO Tutors (Name, Telephone, Address) VALUES (@Name, @Telephone, @Address);
                     SELECT last_insert_rowid();
                     """,
                param: new { tutor.Name, tutor.Telephone, tutor.Address },
                transaction: transaction).ConfigureAwait(false);

            await connection.ExecuteAsync(
                sql: "INSERT INTO PetSitterTutors (PetSitterId, TutorId) VALUES (@PetSitterId, @TutorId)",
                param: new { PetSitterId = petSitterId, TutorId = tutorId },
                transaction: transaction).ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>Every tutor belonging to the given pet sitter, alphabetically.</summary>
    public async Task<Tutors[]> ListForPetSitterAsync(int petSitterId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        var tutors = await connection.QueryAsync<Tutors>(
            sql: """
                 SELECT t.TutorId, t.Name, t.Telephone, t.Address
                 FROM Tutors t
                 INNER JOIN PetSitterTutors pst ON pst.TutorId = t.TutorId
                 WHERE pst.PetSitterId = @PetSitterId
                 ORDER BY t.Name
                 """,
            param: new { PetSitterId = petSitterId }).ConfigureAwait(false);

        return [.. tutors];
    }

    public async Task<Tutors?> GetAsync(int tutorId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<Tutors>(
            sql: "SELECT TutorId, Name, Telephone, Address FROM Tutors WHERE TutorId = @TutorId",
            param: new { TutorId = tutorId }).ConfigureAwait(false);
    }

    public override Task<Response> Add(Tutors entity) =>
        throw new NotSupportedException($"Use {nameof(AddForPetSitterAsync)} so the tutor is linked to an account.");

    /// <summary>
    /// Removes the tutor along with everything that hangs off them — their dogs, those dogs'
    /// services, and the account link — in one transaction, since nothing cascades in the schema.
    /// </summary>
    public override async Task<Response> Delete(int entityId)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

            await connection.ExecuteAsync(
                sql: """
                     DELETE FROM WalkingService WHERE DogId IN (SELECT DogId FROM Dogs WHERE TutorId = @TutorId);
                     DELETE FROM PetSittingService WHERE DogId IN (SELECT DogId FROM Dogs WHERE TutorId = @TutorId);
                     DELETE FROM PetHotelService WHERE DogId IN (SELECT DogId FROM Dogs WHERE TutorId = @TutorId);
                     DELETE FROM Dogs WHERE TutorId = @TutorId;
                     DELETE FROM PetSitterTutors WHERE TutorId = @TutorId;
                     DELETE FROM Tutors WHERE TutorId = @TutorId;
                     """,
                param: new { TutorId = entityId },
                transaction: transaction).ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    public override Task<Response> AddMany(Tutors entity) => throw new NotImplementedException();

    public override Task<Tutors> Get(int entityId) => throw new NotImplementedException();

    public override void GetAll(Action<Tutors[]> onComplete, Action<Exception>? onError = null) =>
        throw new NotSupportedException($"Use {nameof(ListForPetSitterAsync)} — tutors are scoped to an account.");

    /// <summary>
    /// Saves an edit from the tutor detail screen. The account link in PetSitterTutors is
    /// untouched — editing a tutor never moves them to another pet sitter.
    /// </summary>
    public override async Task<Response> Update(Tutors entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: """
                     UPDATE Tutors
                     SET Name = @Name, Telephone = @Telephone, Address = @Address
                     WHERE TutorId = @TutorId
                     """,
                param: new { entity.TutorId, entity.Name, entity.Telephone, entity.Address }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    public override Task<Response> DeleteAll() => throw new NotImplementedException();
}