using System.Collections.Generic;
using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.DependencyInversion;

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