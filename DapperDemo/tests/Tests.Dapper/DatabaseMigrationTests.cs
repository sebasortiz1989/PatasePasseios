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

    /// <summary>
    /// The four service tables in the shape that shipped before ServiceDone existed. Separate from
    /// <see cref="CreateLegacyDatabase"/> so the tests that only care about the account stay small.
    /// </summary>
    private void CreateLegacyServiceTables()
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        connection.Execute(
            """
            CREATE TABLE WalkingService (
                WalkingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                PetSitterId INTEGER NOT NULL,
                Date DATETIME NOT NULL,
                Price DECIMAL(10, 2) NOT NULL,
                ServicePaid BOOLEAN);

            CREATE TABLE PetSittingService (
                PetSittingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                PetSitterId INTEGER NOT NULL,
                Date DATETIME NOT NULL,
                Price DECIMAL(10, 2) NOT NULL,
                ServicePaid BOOLEAN);

            CREATE TABLE PetHotelService (
                PetHotelServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                PetSitterId INTEGER NOT NULL,
                StartDate DATETIME NOT NULL,
                EndDate DATETIME NOT NULL,
                PricePerDay DECIMAL(10, 2) NOT NULL,
                RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
                ServicePaid BOOLEAN);

            CREATE TABLE DayCareService (
                DayCareServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                PetSitterId INTEGER NOT NULL,
                Date DATETIME NOT NULL,
                Price DECIMAL(10, 2) NOT NULL,
                RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
                ServicePaid BOOLEAN);
            """);
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

    /// <summary>
    /// ServiceDone arrived after the service tables shipped, so it has to reach a database that
    /// already has them — on all four, since the agenda reads every one of them.
    /// </summary>
    [Fact]
    public void OpeningAnOlderDatabaseAddsServiceDoneToEveryServiceTable()
    {
        CreateLegacyDatabase();
        CreateLegacyServiceTables();

        _ = new DapperDatabaseService(path);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();

        foreach (var table in new[] { "WalkingService", "PetSittingService", "PetHotelService", "DayCareService" })
        {
            var columns = connection.Query<string>($"SELECT name FROM pragma_table_info('{table}')").ToArray();
            Assert.Contains("ServiceDone", columns);
        }
    }

    /// <summary>
    /// A booking that predates the column is not retroactively declared done. Its date is in the
    /// past but that is no evidence the sitter turned up, so it comes back as still to do.
    /// </summary>
    [Fact]
    public void ABookingFromBeforeTheColumnExistedIsNotMarkedDone()
    {
        CreateLegacyDatabase();
        CreateLegacyServiceTables();

        using (var seed = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            seed.Open();
            seed.Execute(
                "INSERT INTO WalkingService (DogId, PetSitterId, Date, Price, ServicePaid) VALUES (1, 1, @Date, 40, 1)",
                new { Date = new DateTime(2020, 3, 1, 9, 0, 0) });
        }

        _ = new DapperDatabaseService(path);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        var done = connection.ExecuteScalar<bool>("SELECT ServiceDone FROM WalkingService");
        var paid = connection.ExecuteScalar<bool>("SELECT ServicePaid FROM WalkingService");

        Assert.False(done);
        Assert.True(paid);
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