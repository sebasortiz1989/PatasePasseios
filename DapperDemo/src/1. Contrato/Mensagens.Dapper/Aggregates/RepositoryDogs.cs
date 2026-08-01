using Dapper;
using Verion.Treinamento.Mensagens.Dapper.Dtos;
using Verion.Treinamento.Mensagens.Dapper.Services;

namespace Verion.Treinamento.Mensagens.Dapper.Aggregates;

public class RepositoryDogs(DapperDatabaseService dapperDatabaseService) : RepositoryBase<Dogs>(dapperDatabaseService)
{
    public override Task<Response> Add(Dogs petSitter)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> AddMany(Dogs entity)
    {
        throw new NotImplementedException();
    }

    public override Task<Dogs> Get(int entityId)
    {
        throw new NotImplementedException();
    }

    public override void GetAll(Action<Dogs[]> onComplete, Action<Exception>? onError = null)
    {
        Task.Run(async () =>
        {
            try
            {
                using (var connection = DapperDatabaseService.Connection)
                {
                    await connection.OpenAsync().NoSync();
                    var petSitters = await connection.QueryAsync<Dogs>("SELECT * FROM Dogs").NoSync();
                    var petSitterModelos = petSitters as Dogs[] ?? [.. petSitters];
                    var result = petSitterModelos.Length != 0 ?
                        petSitterModelos.Select(x => new Dogs
                        {
                            Name = x.Name,
                            TutorId = x.TutorId,
                            Description = x.Description,
                            DogId = x.DogId,
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

    public override Task<Response> Update(Dogs entity)
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
}