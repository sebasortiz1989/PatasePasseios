using System.IO.Compression;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// Restoring a backup written by an older build. Every column added since then is additive with a
/// default, so the archive itself stays readable — the question these answer is whether the app
/// can still <i>use</i> the database once it has been put back.
/// </summary>
public sealed class BackupCompatibilityTests : IDisposable
{
    private readonly string archivePath = Path.Combine(
        Path.GetTempPath(),
        $"dapperdemo-oldbackup-{Guid.NewGuid():N}.zip");

    private readonly string oldDatabasePath = Path.Combine(
        Path.GetTempPath(),
        $"dapperdemo-oldbackup-{Guid.NewGuid():N}.db");

    /// <summary>
    /// Builds a database the way a build before the discount column would have left it, and packs
    /// it into an archive of the shape <see cref="BackupArchive"/> writes.
    /// </summary>
    private async Task<string> WriteOldBackupAsync(int? stampVersion = null)
    {
        // The real schema, then the column removed again — the closest thing to an old file
        // without keeping a copy of last month's DDL around.
        _ = new DapperDatabaseService(oldDatabasePath);
        SqliteConnection.ClearAllPools();

        var builder = new SqliteConnectionStringBuilder { DataSource = oldDatabasePath, Pooling = false };
        using (var connection = new SqliteConnection(builder.ToString()))
        {
            await connection.OpenAsync();

            foreach (var table in new[] { "WalkingService", "PetSittingService", "PetHotelService", "DayCareService" })
            {
                using var drop = connection.CreateCommand();
                drop.CommandText = $"ALTER TABLE {table} DROP COLUMN Discount";
                await drop.ExecuteNonQueryAsync();
            }

            // A booking made by that older build, so the restore has something to carry.
            using var seed = connection.CreateCommand();
            seed.CommandText = """
                INSERT INTO Tutors (TutorId, Name, Telephone, Address) VALUES (900, 'Marina', '9', 'Centro');
                INSERT INTO PetSitterTutors (PetSitterId, TutorId) VALUES (1, 900);
                INSERT INTO Dogs (DogId, TutorId, Name, Breed) VALUES (900, 900, 'Tobias', 'SRD');
                INSERT INTO WalkingService (DogId, PetSitterId, Date, Price, ServicePaid, ServiceDone)
                VALUES (900, 1, '2026-08-21 09:00:00', 60, 0, 1);
                """;
            await seed.ExecuteNonQueryAsync();

            if (stampVersion is int forced)
            {
                using var stamp = connection.CreateCommand();
                stamp.CommandText = $"PRAGMA user_version = {forced}";
                await stamp.ExecuteNonQueryAsync();
            }
        }

        SqliteConnection.ClearAllPools();

        using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(oldDatabasePath, "DapperDemo.db");
        }

        return archivePath;
    }

    [Fact]
    public async Task AnArchiveFromBeforeTheDiscountColumnIsStillAccepted()
    {
        using var live = new TestDatabase();
        var backup = await WriteOldBackupAsync();

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            Assert.Equal(Response.Successful, await new BackupArchive(live.Database).RestoreFromAsync(source));
        }
    }

    [Fact]
    public async Task TheAgendaStillReadsAfterRestoringSuchAnArchive()
    {
        using var live = new TestDatabase();
        var petSitterId = await live.SeedPetSitterAsync();
        var backup = await WriteOldBackupAsync();

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            await new BackupArchive(live.Database).RestoreFromAsync(source);
        }

        // The restore replaced the file underneath a service whose constructor already ran. Before
        // the migration was re-run here, this threw "no such column: w.Discount" — and it threw on
        // the screen the user lands on straight after the restore tells them to sign in again.
        var services = await live.Services.ListForPetSitterAsync(petSitterId);

        Assert.Single(services);
    }

    [Fact]
    public async Task ABookingFromTheOlderBuildKeepsItsFullPrice()
    {
        using var live = new TestDatabase();
        var petSitterId = await live.SeedPetSitterAsync();
        var backup = await WriteOldBackupAsync();

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            await new BackupArchive(live.Database).RestoreFromAsync(source);
        }

        var walk = Assert.Single(await live.Services.ListForPetSitterAsync(petSitterId));

        // The column arrives with its default, so a service booked before discounts existed is
        // undiscounted rather than free.
        Assert.Equal(0m, walk.Discount);
        Assert.Equal(0m, walk.DiscountAmount);
        Assert.Equal(60m, walk.Total);
        Assert.Equal(60m, walk.AmountDue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task AnArchiveFromAnIncompatibleSchemaIsRefused(int version)
    {
        // Below: the file predates a layout change, and the launch-time check would drop every
        // table on the next start. Above: written by a build whose tables these queries never saw.
        using var live = new TestDatabase();
        var backup = await WriteOldBackupAsync(stampVersion: version);

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            Assert.Equal(
                Response.IncompatibleVersion,
                await new BackupArchive(live.Database).RestoreFromAsync(source));
        }
    }

    [Fact]
    public async Task RefusingAnArchiveLeavesTheDeviceUntouched()
    {
        using var live = new TestDatabase();
        var (petSitterId, _, dogId) = await live.SeedAccountAsync();
        await live.Services.AddSittingAsync(new DapperDemo.Repository.Dapper.Dtos.PetSittingService
        {
            DogId = dogId, PetSitterId = petSitterId, Date = new DateTime(2026, 8, 1, 9, 0, 0), Price = 120m,
        });

        var backup = await WriteOldBackupAsync(stampVersion: 1);

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            await new BackupArchive(live.Database).RestoreFromAsync(source);
        }

        // The refusal happens before anything is copied over, so what was here is still here.
        var kept = Assert.Single(await live.Services.ListForPetSitterAsync(petSitterId));
        Assert.Equal(120m, kept.Total);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { archivePath, oldDatabasePath })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}