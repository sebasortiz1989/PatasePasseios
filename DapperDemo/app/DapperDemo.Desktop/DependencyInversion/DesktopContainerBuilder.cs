using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace DapperDemo.Desktop.DependencyInversion
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