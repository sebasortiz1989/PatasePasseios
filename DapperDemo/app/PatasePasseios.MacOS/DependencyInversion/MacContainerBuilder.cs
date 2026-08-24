using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace PatasePasseios.MacOS.DependencyInversion
{
    internal sealed class MacContainerBuilder : ImmutableContainerBuilder
    {
        public MacContainerBuilder()
            : base(GetBuilders())
        {
        }

        private static IEnumerable<ContainerBuilder> GetBuilders()
        {
            yield return new PatasePasseiosInfrastructureContainerBuilder();
        }
    }
}