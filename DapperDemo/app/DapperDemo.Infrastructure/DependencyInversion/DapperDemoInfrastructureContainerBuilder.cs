using AvaloniaFramework.DependencyInjection;
using DapperDemo.Infrastructure.Services;
using DapperDemo.Repository.Dapper.Aggregates;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.View.DependencyInversion;
using DapperDemo.Viewmodel.Services;

namespace DapperDemo.Infrastructure.DependencyInversion;

public class DapperDemoInfrastructureContainerBuilder : ImmutableContainerBuilder
{
    public DapperDemoInfrastructureContainerBuilder()
        : base(GetBuilders())
    {
    }

    private static IEnumerable<ContainerBuilder> GetBuilders()
    {
        yield return new DapperDemoViewContainerBuilder();
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

        // The stand-in destination until there is a Google Drive client id to sign in against.
        // Swapping in the real store is this one line — everything above and around it is already
        // written against the CloudBackupStore abstraction.
        yield return CreateSingleton<LocalFolderBackupStore>().WithAbstractions();
        yield return CreateSingleton<CloudBackupService>();
    }
}