using System.IO.Compression;
using PatasePasseios.Repository.Dapper.Dtos;
using PatasePasseios.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// Reading the database straight after a restore, in the same process that was using the old one —
/// which is what the app does: it restores, signs the user out, and they sign straight back in.
/// </summary>
public sealed class RestoreStaleReadTests : IDisposable
{
    private readonly string archivePath = Path.Combine(Path.GetTempPath(), $"dd-stale-{Guid.NewGuid():N}.zip");
    private readonly string donorPath = Path.Combine(Path.GetTempPath(), $"dd-stale-{Guid.NewGuid():N}.db");

    /// <summary>An archive whose database carries an account the live one has never seen.</summary>
    private async Task<string> WriteBackupWithAccountAsync(string email)
    {
        var donor = new DapperDatabaseService(donorPath);
        var petSitters = new PatasePasseios.Repository.Dapper.Aggregates.RepositoryPetSitter(donor);
        await petSitters.Add(new PetSitter
        {
            Email = email,
            Password = "8998",
            PasswordHash = string.Empty,
            Name = "Larissa Lopes",
            BirthDate = new DateTime(1990, 1, 1),
        });

        SqliteConnection.ClearAllPools();

        using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(donorPath, "DapperDemo.db");
        }

        return archivePath;
    }

    [Fact]
    public async Task AnAccountFromTheRestoredBackupCanBeFoundImmediately()
    {
        const string Email = "larivlopes@hotmail.com";

        using var live = new TestDatabase();

        // The app has been running and reading: the pool is warm before the restore.
        Assert.Null(await live.PetSitters.GetByEmailAsync(Email));

        var backup = await WriteBackupWithAccountAsync(Email);

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            Assert.Equal(
                PatasePasseios.Repository.Dapper.Response.Successful,
                await new BackupArchive(live.Database).RestoreFromAsync(source));
        }

        // The sign-in the user does straight afterwards, through the same service instance.
        var restored = await live.PetSitters.GetByEmailAsync(Email);

        Assert.NotNull(restored);
        Assert.Equal("Larissa Lopes", restored!.Name);
    }

    [Theory]
    [InlineData("larivlopes@hotmail.com")]
    [InlineData("  larivlopes@hotmail.com  ")]
    [InlineData("Larivlopes@Hotmail.com")]
    [InlineData("LARIVLOPES@HOTMAIL.COM")]
    public async Task TheAccountIsFoundHoweverTheEmailIsTyped(string typed)
    {
        // An address is not case-sensitive, and a paste brings spaces. Answering "this e-mail does
        // not exist" for an account that plainly does is indistinguishable from a failed restore.
        const string Stored = "larivlopes@hotmail.com";

        using var live = new TestDatabase();
        var backup = await WriteBackupWithAccountAsync(Stored);

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            await new BackupArchive(live.Database).RestoreFromAsync(source);
        }

        Assert.Equal(PatasePasseios.Repository.Dapper.Response.Successful, live.PetSitters.VerifyLogin(typed, "8998"));
        Assert.NotNull(await live.PetSitters.GetByEmailAsync(typed));
    }

    [Fact]
    public async Task AnAddressThatIsGenuinelyAbsentStillReportsMissing()
    {
        using var live = new TestDatabase();
        var backup = await WriteBackupWithAccountAsync("larivlopes@hotmail.com");

        var source = File.OpenRead(backup);
        await using (source.ConfigureAwait(false))
        {
            await new BackupArchive(live.Database).RestoreFromAsync(source);
        }

        Assert.Equal(
            PatasePasseios.Repository.Dapper.Response.EmailDoesNotExists,
            live.PetSitters.VerifyLogin("someone@else.com", "8998"));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { archivePath, donorPath })
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