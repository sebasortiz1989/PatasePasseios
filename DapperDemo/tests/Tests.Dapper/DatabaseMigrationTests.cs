using Dapper;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The additive migration, which is the part of the schema work that can lose someone's records
/// if it goes wrong.
/// </summary>
/// <remarks>
/// The CREATE TABLE statements are all IF NOT EXISTS, so a new column never reaches a database
/// that already exists. Bumping the schema version would pick it up only by dropping every table,
/// so columns are added with ALTER instead — and these tests hold that line.
/// </remarks>
public class DatabaseMigrationTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"dapperdemo-migration-{Guid.NewGuid():N}.db");

    /// <summary>
    /// A database in the shape that shipped before Pix and HideMoney existed, with a real account
    /// in it, so opening it exercises the upgrade path rather than a fresh create.
    /// </summary>
    private void CreateLegacyDatabase()
    {
        SQLitePCL.Batteries.Init();

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        connection.Execute(
            """
            CREATE TABLE PetSitter (
                PetSitterId INTEGER PRIMARY KEY AUTOINCREMENT,
                Email VARCHAR(255) NOT NULL UNIQUE,
                PasswordHash VARCHAR(255) NOT NULL,
                Name VARCHAR(100) NOT NULL,
                BirthDate DATETIME);
            """);
        connection.Execute(
            "INSERT INTO PetSitter (Email, PasswordHash, Name, BirthDate) VALUES (@Email, @Hash, @Name, @BirthDate)",
            new { Email = "antiga@test.com", Hash = "hash-antigo", Name = "Conta Antiga", BirthDate = new DateTime(1990, 5, 2) });

        // Already current, so the drop-everything path must not fire.
        connection.Execute($"PRAGMA user_version = {DapperDatabaseService.CurrentSchemaVersion};");
    }

    [Fact]
    public void OpeningAnOlderDatabaseAddsTheMissingColumns()
    {
        CreateLegacyDatabase();

        _ = new DapperDatabaseService(path);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        var columns = connection.Query<string>("SELECT name FROM pragma_table_info('PetSitter')").ToArray();

        Assert.Contains("Pix", columns);
        Assert.Contains("HideMoney", columns);
    }

    /// <summary>The whole point of an ALTER over a version bump: the records survive.</summary>
    [Fact]
    public async Task OpeningAnOlderDatabaseKeepsTheExistingAccount()
    {
        CreateLegacyDatabase();

        var service = new DapperDatabaseService(path);
        var repository = new DapperDemo.Repository.Dapper.Aggregates.RepositoryPetSitter(service);
        var account = await repository.GetByEmailAsync("antiga@test.com");

        Assert.NotNull(account);
        Assert.Equal("Conta Antiga", account!.Name);
        Assert.Equal("hash-antigo", account.PasswordHash);
        Assert.Null(account.Pix);
        Assert.False(account.HideMoney);
    }

    /// <summary>Opening the same database twice must not fail on a column that is already there.</summary>
    [Fact]
    public async Task TheMigrationCanRunTwice()
    {
        CreateLegacyDatabase();

        _ = new DapperDatabaseService(path);
        _ = new DapperDatabaseService(path);

        var service = new DapperDatabaseService(path);
        var repository = new DapperDemo.Repository.Dapper.Aggregates.RepositoryPetSitter(service);
        var account = await repository.GetByEmailAsync("antiga@test.com");

        Assert.NotNull(account);
        Assert.Equal("Conta Antiga", account!.Name);
    }

    /// <summary>A fresh file gets the whole schema, including the four service tables.</summary>
    [Fact]
    public void AFreshDatabaseGetsEveryTable()
    {
        _ = new DapperDatabaseService(path);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        var tables = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'").ToArray();

        Assert.Contains("PetSitter", tables);
        Assert.Contains("Tutors", tables);
        Assert.Contains("PetSitterTutors", tables);
        Assert.Contains("Dogs", tables);
        Assert.Contains("WalkingService", tables);
        Assert.Contains("PetSittingService", tables);
        Assert.Contains("PetHotelService", tables);
        Assert.Contains("DayCareService", tables);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Left behind in the system temp folder rather than failing a passing test.
        }
    }
}