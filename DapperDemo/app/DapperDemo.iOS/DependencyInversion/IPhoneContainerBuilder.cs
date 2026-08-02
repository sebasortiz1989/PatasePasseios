using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace DapperDemo.iOS.DependencyInversion;

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