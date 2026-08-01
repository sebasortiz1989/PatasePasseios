using System.Collections.Generic;
using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.Infrastructure.DependencyInversion;

namespace Verion.Treinamento.DapperDemo.Android.DependencyInversion;

public sealed class DroidContainerBuilder : ImmutableContainerBuilder
{
    public DroidContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new DapperDemoInfrastructureContainerBuilder();
        yield return new ImmutableContainerBuilder(GetRegistrations());
    }

    private static IEnumerable<ContainerRegistration> GetRegistrations()
    {
        yield return CreateSingleton<ServicoNumeroSerieDispositivoDroid>();
    }
}