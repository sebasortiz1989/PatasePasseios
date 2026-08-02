using Dapper;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Mensagens.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Mensagens.Dapper.Aggregates;

public sealed class RepositoryPetSitter : RepositoryBase<PetSitter>
{
    public RepositoryPetSitter(DapperDatabaseService dapperDatabaseService)
        : base(dapperDatabaseService)
    {
    }

    public override async Task<Response> Add(PetSitter petSitter)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(petSitter.Password);
            await connection.ExecuteAsync(
                sql: "INSERT INTO PetSitter (Email, PasswordHash, Name, BirthDate) VALUES (@Email, @PasswordHash, @Name, @BirthDate)",
                param: new { petSitter.Email, PasswordHash = hashedPassword, petSitter.Name, petSitter.BirthDate }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return e.Message == "SQLite Error 19: 'UNIQUE constraint failed: PetSitter.Email'." ? Response.EmailExists : Response.Failed;
        }
    }

    public override Task<Response> AddMany(PetSitter petSitter)
    {
        throw new NotImplementedException();
    }

    public override Task<PetSitter> Get(int entityId)
    {
        throw new NotImplementedException();
    }

    public override void GetAll(Action<PetSitter[]> onComplete, Action<Exception>? onError = null)
    {
        Task.Run(async () =>
        {
            try
            {
                using (var connection = DapperDatabaseService.Connection)
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    var petSitters = await connection.QueryAsync<PetSitter>("SELECT * FROM PetSitter").ConfigureAwait(false);
                    var petSitterModelos = petSitters as PetSitter[] ?? petSitters.ToArray();
                    var result = petSitterModelos.Length != 0 ?
                        petSitterModelos.Select(x => new PetSitter
                        {
                            Name = x.Name,
                            BirthDate = x.BirthDate,
                            Email = x.Email,
                            PasswordHash = x.PasswordHash,
                            PetSitterId = x.PetSitterId,
                        }).ToArray() :
                        [];

                    onComplete(result);
                }
            }
#pragma warning disable CA1031 // The onError callback is this method's only error channel: it runs
            // on a background task with no caller to rethrow to, so narrowing the catch would lose
            // failures silently rather than reporting them.
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
#pragma warning restore CA1031
        });
    }

    public override Task<Response> Update(PetSitter petSitter)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> Delete(int entityId)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> DeleteAll()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The logged-in row, so the app can scope data to this account and greet the user by name.
    /// <see cref="VerifyLogin"/> only reports success, which isn't enough on its own.
    /// </summary>
    public async Task<PetSitter?> GetByEmailAsync(string email)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<PetSitter>(
            sql: "SELECT PetSitterId, Email, PasswordHash, Name, BirthDate FROM PetSitter WHERE Email = @Email",
            param: new { Email = email }).ConfigureAwait(false);
    }

    public Response VerifyLogin(string email, string password)
    {
        using (var connection = DapperDatabaseService.Connection)
        {
            connection.Open();
            var petSitter = connection.QueryFirstOrDefault<PetSitter>("SELECT * FROM PetSitter WHERE Email = @Email", new { Email = email });

            if (petSitter is { PasswordHash: not null })
            {
                return BCrypt.Net.BCrypt.Verify(password, petSitter.PasswordHash) ? Response.Successful : Response.WrongPassword;
            }

            return Response.EmailDoesNotExists;
        }
    }
}