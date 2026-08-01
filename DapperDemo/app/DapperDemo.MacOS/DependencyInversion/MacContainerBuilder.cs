using System.Collections.Generic;
using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.Infrastructure.DependencyInversion;

namespace Verion.Treinamento.DapperDemo.MacOS.DependencyInversion
{
    internal sealed class MacContainerBuilder : ImmutableContainerBuilder
    {
        public MacContainerBuilder()
            : base(GetBuilders())
        {
        }

        private static IEnumerable<ContainerBuilder> GetBuilders()
        {
            yield return new DapperDemoInfrastructureContainerBuilder();
        }
    }
}