using System.Collections.Generic;
using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;

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