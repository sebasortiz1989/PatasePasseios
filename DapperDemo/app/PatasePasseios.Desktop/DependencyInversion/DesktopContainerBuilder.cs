using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace PatasePasseios.Desktop.DependencyInversion
{
    internal sealed class DesktopContainerBuilder : ImmutableContainerBuilder
    {
        public DesktopContainerBuilder()
            : base(GetBuilders())
        {
        }

        private static IEnumerable<ContainerBuilder> GetBuilders()
        {
            yield return new PatasePasseiosInfrastructureContainerBuilder();
            yield return new ImmutableContainerBuilder(GetRegistrations());
        }

        private static IEnumerable<ContainerRegistration> GetRegistrations()
        {
            yield return CreateSingleton(new ServicoNumeroSerieDispositivo());
        }
    }
}