using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;
using System.Collections.Generic;

namespace DapperDemo.Android.DependencyInversion;

public sealed class DroidContainerBuilder : ImmutableContainerBuilder
{
    public DroidContainerBuilder()
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
        yield return CreateSingleton<ServicoNumeroSerieDispositivoDroid>();
    }
}