using AvaloniaFramework.DependencyInjection;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Services;
using DapperDemo.View.DependencyInversion;

namespace DapperDemo.Infrastructure.DependencyInversion;

public class DapperDemoInfrastructureContainerBuilder : ImmutableContainerBuilder
{
    public DapperDemoInfrastructureContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new DapperDemoViewContainerBuilder();
        yield return new ImmutableContainerBuilder(GetRegistrations());
    }

    private static IEnumerable<ContainerRegistration> GetRegistrations()
    {
        yield return CreateSingleton<RepositoryPetSitter>();
        yield return CreateSingleton<RepositoryDogs>();
        yield return CreateSingleton<RepositoryTutors>();
        yield return CreateSingleton<RepositoryServices>();
        yield return CreateSingleton<DapperDatabaseService>();
    }
}