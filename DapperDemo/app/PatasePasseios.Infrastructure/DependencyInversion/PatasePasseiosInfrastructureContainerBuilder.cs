using AvaloniaFramework.DependencyInjection;
using PatasePasseios.Repository.Dapper.Aggregates;
using PatasePasseios.Repository.Dapper.Services;
using PatasePasseios.View.DependencyInversion;
using PatasePasseios.Viewmodel.Services;

namespace PatasePasseios.Infrastructure.DependencyInversion;

public class PatasePasseiosInfrastructureContainerBuilder : ImmutableContainerBuilder
{
    public PatasePasseiosInfrastructureContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new PatasePasseiosViewContainerBuilder();
        yield return new ImmutableContainerBuilder(GetRegistrations());
    }

    private static IEnumerable<ContainerRegistration> GetRegistrations()
    {
        yield return CreateSingleton<RepositoryPetSitter>();
        yield return CreateSingleton<RepositoryDogs>();
        yield return CreateSingleton<RepositoryTutors>();
        yield return CreateSingleton<RepositoryServices>();
        yield return CreateSingleton<RepositoryPayments>();
        yield return CreateSingleton<DapperDatabaseService>();
        yield return CreateSingleton<BackupArchive>();
        yield return CreateSingleton<CloudBackupState>();
        yield return CreateSingleton<DisplayPreferencesStore>();

        yield return CreateSingleton<CloudBackupService>();
    }
}