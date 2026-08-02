using Dapper;
using Microsoft.Data.Sqlite;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Mensagens.Dapper.Services;

namespace DapperDemo.Mensagens.Dapper.Aggregates;

/// <summary>
/// A dog reaches an account through its tutor, so the scoped reads here join
/// Dogs -> Tutors -> PetSitterTutors rather than filtering on the dog directly.
/// </summary>
public class RepositoryDogs(DapperDatabaseService dapperDatabaseService) : RepositoryBase<Dogs>(dapperDatabaseService)
{
    public override async Task<Response> Add(Dogs dog)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: """
                     INSERT INTO Dogs (TutorId, Name, Breed, Description)
                     VALUES (@TutorId, @Name, @Breed, @Description)
                     """,
                param: new { dog.TutorId, dog.Name, dog.Breed, dog.Description }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>Every dog belonging to the given pet sitter's tutors, alphabetically.</summary>
    public async Task<Dogs[]> ListForPetSitterAsync(int petSitterId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        var dogs = await connection.QueryAsync<Dogs>(
            sql: """
                 SELECT d.DogId, d.TutorId, d.Name, d.Breed, d.Description
                 FROM Dogs d
                 INNER JOIN PetSitterTutors pst ON pst.TutorId = d.TutorId
                 WHERE pst.PetSitterId = @PetSitterId
                 ORDER BY d.Name
                 """,
            param: new { PetSitterId = petSitterId }).ConfigureAwait(false);

        return [.. dogs];
    }

    /// <summary>The dogs of one tutor, used by the tutor detail screen.</summary>
    public async Task<Dogs[]> ListForTutorAsync(int tutorId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        var dogs = await connection.QueryAsync<Dogs>(
            sql: "SELECT DogId, TutorId, Name, Breed, Description FROM Dogs WHERE TutorId = @TutorId ORDER BY Name",
            param: new { TutorId = tutorId }).ConfigureAwait(false);

        return [.. dogs];
    }

    public async Task<Dogs?> GetAsync(int dogId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<Dogs>(
            sql: "SELECT DogId, TutorId, Name, Breed, Description FROM Dogs WHERE DogId = @DogId",
            param: new { DogId = dogId }).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the dog together with every service booked for it, in one transaction — the
    /// service tables carry no ON DELETE CASCADE, so leaving them behind would strand rows
    /// that the agenda joins against.
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
                     DELETE FROM WalkingService WHERE DogId = @DogId;
                     DELETE FROM PetSittingService WHERE DogId = @DogId;
                     DELETE FROM PetHotelService WHERE DogId = @DogId;
                     DELETE FROM Dogs WHERE DogId = @DogId;
                     """,
                param: new { DogId = entityId },
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

    public override Task<Response> AddMany(Dogs entity) => throw new NotImplementedException();

    public override Task<Dogs> Get(int entityId) => throw new NotImplementedException();

    public override void GetAll(Action<Dogs[]> onComplete, Action<Exception>? onError = null) =>
        throw new NotSupportedException($"Use {nameof(ListForPetSitterAsync)} — dogs are scoped to an account.");

    public override Task<Response> Update(Dogs entity) => throw new NotImplementedException();

    public override Task<Response> DeleteAll() => throw new NotImplementedException();
}
