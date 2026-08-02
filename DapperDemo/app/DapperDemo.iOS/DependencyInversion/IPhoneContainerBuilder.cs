using System.Collections.Generic;
using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;

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