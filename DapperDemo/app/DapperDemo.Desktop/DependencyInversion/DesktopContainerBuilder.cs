using System.Collections.Generic;
using Verion.Infraestrutura.Dependency;
using Verion.Treinamento.DapperDemo.Infrastructure.DependencyInversion;

namespace Verion.Treinamento.DapperDemo.Desktop.DependencyInversion
{
    internal sealed class DesktopContainerBuilder : ImmutableContainerBuilder
    {
        public DesktopContainerBuilder()
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
            yield return CreateSingleton(new ServicoNumeroSerieDispositivo());
        }
    }
}