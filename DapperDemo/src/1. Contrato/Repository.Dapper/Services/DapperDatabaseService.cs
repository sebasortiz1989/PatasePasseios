using Dapper;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Services;

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
        : this(Path.Combine(AppStorage.Folder, "DapperDemo.db"))
    {
    }

    /// <summary>
    /// Opens a database at a given path rather than the app's own.
    /// </summary>
    /// <remarks>
    /// The parameterless constructor is what the app uses. This one exists so the tests can drive
    /// the real schema and the real repositories against a throwaway file instead of the records
    /// on the machine running them.
    /// </remarks>
    /// <param name="databasePath">Where the SQLite file lives. Created if it does not exist.</param>
    public DapperDatabaseService(string databasePath)
    {
        SQLitePCL.Batteries.Init();
        InitializeDatabase(databasePath);
    }

    /// <summary>Gets the schema this build expects, stamped into a backup so a restore can check it.</summary>
    public static int CurrentSchemaVersion => SchemaVersion;

    public SqliteConnection Connection => new(connectionString);

    /// <summary>Gets the database file on disk. Public so backup and restore can copy over it.</summary>
    public string DatabasePath { get; private set; } = string.Empty;

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
                 DROP TABLE IF EXISTS DayCareService;
                 DROP TABLE IF EXISTS Dogs;
                 DROP TABLE IF EXISTS PetSitterTutors;
                 DROP TABLE IF EXISTS Tutors;
                 DROP TABLE IF EXISTS PetSitter;
                 """);

        connection.Execute($"PRAGMA user_version = {SchemaVersion};");
    }

    /// <summary>
    /// Adds columns introduced after a table first shipped.
    /// </summary>
    /// <remarks>
    /// The CREATE TABLE statements below are all IF NOT EXISTS, so they do nothing to a database
    /// that already has the table — a new column would never appear. Bumping
    /// <see cref="SchemaVersion"/> would pick it up, but only by dropping every table and taking
    /// the user's records with it. An ALTER is the additive route: existing rows keep their data
    /// and get NULL for the new column.
    /// </remarks>
    private static void AddMissingColumns(SqliteConnection connection)
    {
        AddColumnIfMissing(connection, "PetSitter", "Pix", "VARCHAR(255)");
        AddColumnIfMissing(connection, "PetSitter", "HideMoney", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, "Tutors", "Credit", "DECIMAL(10, 2) NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, "PetHotelService", "ExtraCharge", "DECIMAL(10, 2) NOT NULL DEFAULT 0");
    }

    private static void AddColumnIfMissing(SqliteConnection connection, string table, string column, string type)
    {
        // PRAGMA table_info is the only portable way to ask SQLite what a table looks like;
        // there is no ADD COLUMN IF NOT EXISTS.
        var columns = connection.Query<string>($"SELECT name FROM pragma_table_info('{table}')");
        if (columns.Any(name => string.Equals(name, column, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        connection.Execute($"ALTER TABLE {table} ADD COLUMN {column} {type};");
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
                     BirthDate DATETIME,
                     Pix VARCHAR(255),
                     HideMoney INTEGER NOT NULL DEFAULT 0);
                 
                 -- Credit is money the tutor has already handed over beyond what they owed; it
                 -- is spent automatically against the next service booked for one of their dogs.
                 CREATE TABLE IF NOT EXISTS Tutors (
                     TutorId INTEGER PRIMARY KEY AUTOINCREMENT,
                     Name VARCHAR(255) NOT NULL,
                     Telephone VARCHAR(100) NOT NULL,
                     Address VARCHAR(100) NOT NULL,
                     Credit DECIMAL(10, 2) NOT NULL DEFAULT 0);
                 
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
                     -- Anything charged on top of the nightly rate, such as a late pick-up.
                     ExtraCharge DECIMAL(10, 2) NOT NULL DEFAULT 0,
                     RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));

                 -- Day-care is a hotel stay without the span: one Date at midnight, no EndDate,
                 -- and Price is the whole fee for the day rather than a rate to multiply.
                 CREATE TABLE IF NOT EXISTS DayCareService (
                     DayCareServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                     DogId INTEGER NOT NULL,
                     PetSitterId INTEGER NOT NULL,
                     Date DATETIME NOT NULL,
                     Price DECIMAL(10, 2) NOT NULL,
                     RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));
                 """);
    }

    private void InitializeDatabase(string databasePath)
    {
        DatabasePath = databasePath;
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();

        using (var connection = Connection)
        {
            connection.Open();
            RecreateTablesIfSchemaIsStale(connection);
            CreatePetSitterTableIfNotExists(connection);
            AddMissingColumns(connection);
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