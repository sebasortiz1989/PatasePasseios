using Verion.Dominio.Infraestrutura.DependencyInversion;
using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.View.DependencyInversion;
using Verion.Treinamento.Mensagens.Dapper.Aggregates;
using Verion.Treinamento.Mensagens.Dapper.Services;

namespace Verion.Treinamento.DapperDemo.Infrastructure.DependencyInversion;

public class DapperDemoInfrastructureContainerBuilder : ImmutableContainerBuilder
{
    public DapperDemoInfrastructureContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new DomainContainerBuilder();
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