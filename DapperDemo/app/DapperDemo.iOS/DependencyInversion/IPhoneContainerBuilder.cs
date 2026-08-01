using System.Collections.Generic;
using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.Infrastructure.DependencyInversion;

namespace Verion.Treinamento.DapperDemo.iOS.DependencyInversion;

internal sealed class IPhoneContainerBuilder : ImmutableContainerBuilder
{
    public IPhoneContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new DapperDemoInfrastructureContainerBuilder();
    }
}