using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace PatasePasseios.Android.DependencyInversion;

public sealed class DroidContainerBuilder : ImmutableContainerBuilder
{
    public DroidContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new PatasePasseiosInfrastructureContainerBuilder();

        // After the layers above, because a later registration replaces an earlier one for the
        // same service type. This is the seam for anything only a phone can do: the View layer
        // registers a ShareSheet that reports it cannot share, and this head puts the real one
        // over the top of it.
        yield return new ImmutableContainerBuilder(GetRegistrations());
    }

    private static IEnumerable<ContainerRegistration> GetRegistrations()
    {
        yield return CreateSingleton<AndroidShareSheet>().WithAbstractions();
    }
}