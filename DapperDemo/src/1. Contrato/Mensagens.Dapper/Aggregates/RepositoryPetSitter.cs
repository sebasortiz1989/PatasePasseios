using Dapper;
using Microsoft.Data.Sqlite;
using Verion.Treinamento.Mensagens.Dapper.Dtos;
using Verion.Treinamento.Mensagens.Dapper.Services;

namespace Verion.Treinamento.Mensagens.Dapper.Aggregates;

public sealed class RepositoryPetSitter : RepositoryBase<PetSitter>
{
    public RepositoryPetSitter(DapperDatabaseService dapperDatabaseService)
        : base(dapperDatabaseService)
    {
    }

    public override Task<Response> Add(PetSitter petSitter)
    {
        try
        {
            using (var connection = DapperDatabaseService.Connection)
            {
                connection.Open();
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(petSitter.Password);
                connection.Execute(
                    sql: "INSERT INTO PetSitter (Email, PasswordHash, Name, BirthDate) VALUES (@Email, @PasswordHash, @Name, @BirthDate)",
                    param: new { petSitter.Email, PasswordHash = hashedPassword, petSitter.Name, petSitter.BirthDate });

                return Task.FromResult(Response.Successful);
            }
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Task.FromResult(e.Message == "SQLite Error 19: 'UNIQUE constraint failed: PetSitter.Email'." ? Response.EmailExists : Response.Failed);
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
                    var petSitters = await connection.QueryAsync<PetSitter>("SELECT * FROM PetSitter").NoSync();
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
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        });
    }

    public override Task<Response> Update(PetSitter petSitter)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> Delete(int petSitterId)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> DeleteAll()
    {
        throw new NotImplementedException();
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