using Dapper;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

namespace Verion.Treinamento.Mensagens.Dapper.Services;

public sealed class DapperDatabaseService
{
    private string connectionString = string.Empty;

    public DapperDatabaseService()
    {
        SQLitePCL.Batteries.Init();
        InitializeDatabase();
    }

    public SqliteConnection Connection => new(connectionString);

    private void InitializeDatabase()
    {
        var appDataFolder = GetAppDataFolder();
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        string databaseFileName = "DapperDemo.db";
        var databasePath = Path.Combine(appDataFolder, databaseFileName);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();

        using (var connection = Connection)
        {
            connection.Open();
            CreatePetSitterTableIfNotExists(connection);
            CreateMockData(connection);
        }
    }

    private string GetAppDataFolder()
    {
        string appDataFolder;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DapperDemo");
        } 
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "DapperDemo");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            appDataFolder = Path.Combine(appDataFolder, ".local", "share", "DapperDemo");
        }
        else
        {
            appDataFolder = Path.Combine(Environment.CurrentDirectory, "DapperDemo");
        }

        return appDataFolder;
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
                 
                 CREATE TABLE IF NOT EXISTS Dogs (
                     DogId INTEGER PRIMARY KEY AUTOINCREMENT,
                     TutorId INTEGER NOT NULL,
                     Name VARCHAR(255) NOT NULL UNIQUE,
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
                 
                 CREATE TABLE IF NOT EXISTS PetHotelService (
                     PetHotelServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                     DogId INTEGER NOT NULL,
                     PetSitterId INTEGER NOT NULL,
                     StartDate DATETIME NOT NULL,
                     EndDate DATETIME NOT NULL,
                     Price DECIMAL(10, 2) NOT NULL,
                     ServicePaid BOOLEAN,
                     FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
                     FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));
                 """);
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