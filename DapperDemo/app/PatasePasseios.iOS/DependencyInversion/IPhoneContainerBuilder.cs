using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace PatasePasseios.iOS.DependencyInversion;

internal sealed class IPhoneContainerBuilder : ImmutableContainerBuilder
{
    public IPhoneContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new PatasePasseiosInfrastructureContainerBuilder();
    }
}