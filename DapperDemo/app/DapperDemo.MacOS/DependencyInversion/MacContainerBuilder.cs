using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace DapperDemo.MacOS.DependencyInversion
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