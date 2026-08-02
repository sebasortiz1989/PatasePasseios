using Dapper;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Mensagens.Dapper.Services;

public sealed class DapperDatabaseService
{
    /// <summary>
    /// Bump this whenever the schema below changes. On launch the stored PRAGMA user_version is
    /// compared against it: a lower value means the file on disk predates the current schema, so
    /// the tables are dropped and recreated once and the version is stamped. Later launches match
    /// and nothing is dropped, so records the user enters persist normally.
    /// </summary>
    private const int SchemaVersion = 2;

    private string connectionString = string.Empty;

    public DapperDatabaseService()
    {
        SQLitePCL.Batteries.Init();
        InitializeDatabase();
    }

    public SqliteConnection Connection => new(connectionString);

    /// <summary>
    /// Drops every table when the file on disk was built by an older schema, so the CREATE TABLE
    /// IF NOT EXISTS statements below can lay it out again correctly. Runs at most once per
    /// schema version — see <see cref="SchemaVersion"/>.
    /// </summary>
    private static void RecreateTablesIfSchemaIsStale(SqliteConnection connection)
    {
        var currentVersion = connection.ExecuteScalar<int>("PRAGMA user_version;");
        if (currentVersion >= SchemaVersion)
        {
            return;
        }

        connection.Execute(
            sql: """
                 DROP TABLE IF EXISTS WalkingService;
                 DROP TABLE IF EXISTS PetSittingService;
                 DROP TABLE IF EXISTS PetHotelService;
                 DROP TABLE IF EXISTS Dogs;
                 DROP TABLE IF EXISTS PetSitterTutors;
                 DROP TABLE IF EXISTS Tutors;
                 DROP TABLE IF EXISTS PetSitter;
                 """);

        connection.Execute($"PRAGMA user_version = {SchemaVersion};");
    }

    private static void CreatePetSitterTableIfNotExists(SqliteConnection connection)
    {
        connection.Execute(
            sql: """
                 CREATE TABLE IF NOT EXISTS PetSitter (
                     PetSitterId INTEGER PRIMARY KEY AUTOINCREMENT,
                     Email VARCHAR(255) NOT NULL UNIQUE,
                     PasswordHash VARCHAR(255) NOT NULL,
                     Name VARCHAR(100) NOT NULL,
                     BirthDate DATETIME);
                 
                 CREATE TABLE IF NOT EXISTS Tutors (
                     TutorId INTEGER PRIMARY KEY AUTOINCREMENT,
                     Name VARCHAR(255) NOT NULL,
                     Telephone VARCHAR(100) NOT NULL,
                     Address VARCHAR(100) NOT NULL);
                 
                 CREATE TABLE IF NOT EXISTS PetSitterTutors (
                     PetSitterId INTEGER NOT NULL,
                     TutorId INTEGER NOT NULL,
                     PRIMARY KEY (PetSitterId, TutorId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId),
                     FOREIGN KEY (TutorId) REFERENCES Tutors(TutorId));
                 
                 -- Name is deliberately not UNIQUE: different tutors may each have a "Luna".
                 CREATE TABLE IF NOT EXISTS Dogs (
                     DogId INTEGER PRIMARY KEY AUTOINCREMENT,
                     TutorId INTEGER NOT NULL,
                     Name VARCHAR(255) NOT NULL,
                     Breed VARCHAR(255),
                     Description VARCHAR(255),
                     Image BLOB,
                     FOREIGN KEY (TutorId) REFERENCES Tutors(TutorId));
                 
                 CREATE TABLE IF NOT EXISTS WalkingService (
                     WalkingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                     DogId INTEGER NOT NULL,
                     PetSitterId INTEGER NOT NULL,
                     Date DATETIME NOT NULL,
                     Price DECIMAL(10, 2) NOT NULL,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));
                 
                 CREATE TABLE IF NOT EXISTS PetSittingService (
                     PetSittingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                     DogId INTEGER NOT NULL,
                     PetSitterId INTEGER NOT NULL,
                     Date DATETIME NOT NULL,
                     Price DECIMAL(10, 2) NOT NULL,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));
                 
                 -- PricePerDay, not a total: the agenda and the billing summary both treat a
                 -- hotel stay as a daily rate.
                 CREATE TABLE IF NOT EXISTS PetHotelService (
                     PetHotelServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                     DogId INTEGER NOT NULL,
                     PetSitterId INTEGER NOT NULL,
                     StartDate DATETIME NOT NULL,
                     EndDate DATETIME NOT NULL,
                     PricePerDay DECIMAL(10, 2) NOT NULL,
                     RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));
                 """);
    }

    private void InitializeDatabase()
    {
        string databaseFileName = "DapperDemo.db";
        var databasePath = Path.Combine(AppStorage.Folder, databaseFileName);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();

        using (var connection = Connection)
        {
            connection.Open();
            RecreateTablesIfSchemaIsStale(connection);
            CreatePetSitterTableIfNotExists(connection);
            CreateMockData(connection);
        }
    }

    private void CreateMockData(SqliteConnection connection)
    {
        try
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword("8998");
            connection.Execute(
                "INSERT INTO PetSitter (Email, PasswordHash, Name, BirthDate) VALUES (@Email, @PasswordHash, @Name, @BirthDate)",
                new { Email = "test@test.com", PasswordHash = hashedPassword, Name = "TestUser", BirthDate = DateTime.Now });
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
        }
    }
}