using DapperDemo.Repository.Dapper.Services;

namespace DapperDemo.Repository.Dapper;

public abstract class RepositoryBase<TEntity>(DapperDatabaseService dapperDatabaseService)
    where TEntity : class
{
    protected DapperDatabaseService DapperDatabaseService { get; } = dapperDatabaseService;

    public abstract Task<Response> Add(TEntity petSitter);

    public abstract Task<Response> AddMany(TEntity entity);

    public abstract Task<TEntity> Get(int entityId);

    public abstract void GetAll(Action<TEntity[]> onComplete, Action<Exception>? onError = null);

    public abstract Task<Response> Update(TEntity entity);

    public abstract Task<Response> Delete(int entityId);

    public abstract Task<Response> DeleteAll();
}