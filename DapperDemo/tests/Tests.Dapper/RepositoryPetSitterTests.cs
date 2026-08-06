using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using Xunit;

namespace Tests.Dapper;

/// <summary>
/// The account repository: signing in, editing a profile, and changing a password.
/// </summary>
public class RepositoryPetSitterTests
{
    [Fact]
    public async Task TheSeededAccountSignsInWithItsPassword()
    {
        using var db = new TestDatabase();

        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "8998"));
    }

    [Fact]
    public async Task AWrongPasswordIsRejected()
    {
        using var db = new TestDatabase();

        Assert.Equal(Response.WrongPassword, db.PetSitters.VerifyLogin("test@test.com", "nope"));
    }

    [Fact]
    public async Task AnUnknownEmailIsReported()
    {
        using var db = new TestDatabase();

        Assert.Equal(Response.EmailDoesNotExists, db.PetSitters.VerifyLogin("nobody@test.com", "8998"));
    }

    [Fact]
    public async Task AddingAnAccountStoresAHashRatherThanThePassword()
    {
        using var db = new TestDatabase();

        var response = await db.PetSitters.Add(new PetSitter
        {
            Email = "new@test.com",
            PasswordHash = string.Empty,
            Password = "segredo123",
            Name = "Nova",
            BirthDate = new DateTime(1990, 5, 2),
        });

        Assert.Equal(Response.Successful, response);

        var stored = await db.PetSitters.GetByEmailAsync("new@test.com");
        Assert.NotNull(stored);
        Assert.DoesNotContain("segredo123", stored!.PasswordHash, StringComparison.Ordinal);
        Assert.StartsWith("$2", stored.PasswordHash, StringComparison.Ordinal);
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("new@test.com", "segredo123"));
    }

    /// <summary>The e-mail is the login, so a duplicate has to be reported rather than swallowed.</summary>
    [Fact]
    public async Task ASecondAccountCannotReuseAnEmail()
    {
        using var db = new TestDatabase();

        var response = await db.PetSitters.Add(new PetSitter
        {
            Email = "test@test.com",
            PasswordHash = string.Empty,
            Password = "outra",
            Name = "Duplicada",
            BirthDate = DateTime.Now,
        });

        Assert.Equal(Response.EmailExists, response);
    }

    [Fact]
    public async Task UpdateSavesTheProfileFields()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();
        var before = await db.PetSitters.GetAsync(id);

        var response = await db.PetSitters.Update(new PetSitter
        {
            PetSitterId = id,
            Email = before!.Email,
            PasswordHash = before.PasswordHash,
            Name = "Larissa",
            BirthDate = new DateTime(1992, 3, 4),
            Pix = "larissa@pix.com",
        });

        Assert.Equal(Response.Successful, response);

        var after = await db.PetSitters.GetAsync(id);
        Assert.Equal("Larissa", after!.Name);
        Assert.Equal("larissa@pix.com", after.Pix);
        Assert.Equal(new DateTime(1992, 3, 4), after.BirthDate);
    }

    /// <summary>
    /// The e-mail identifies the account everywhere the app looks one up, so Update deliberately
    /// leaves that column alone — passing a different one must change nothing.
    /// </summary>
    [Fact]
    public async Task UpdateCannotChangeTheEmail()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();
        var before = await db.PetSitters.GetAsync(id);

        await db.PetSitters.Update(new PetSitter
        {
            PetSitterId = id,
            Email = "hijack@test.com",
            PasswordHash = before!.PasswordHash,
            Name = before.Name,
            BirthDate = before.BirthDate,
        });

        var after = await db.PetSitters.GetAsync(id);
        Assert.Equal("test@test.com", after!.Email);
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "8998"));
    }

    /// <summary>Changing a password is its own operation, not a side effect of renaming yourself.</summary>
    [Fact]
    public async Task UpdateLeavesThePasswordAlone()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();
        var before = await db.PetSitters.GetAsync(id);

        await db.PetSitters.Update(new PetSitter
        {
            PetSitterId = id,
            Email = before!.Email,
            PasswordHash = "not-a-real-hash",
            Name = "Renomeada",
            BirthDate = before.BirthDate,
        });

        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "8998"));
    }

    /// <summary>
    /// A replaced password stops signing in. Shown with two changes rather than one because the
    /// seeded password is 8998, which is also the master password — logging in with it after the
    /// change would succeed for that reason and prove nothing about revocation.
    /// </summary>
    [Fact]
    public async Task ChangingThePasswordSwapsWhichOneSignsIn()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();

        Assert.Equal(Response.Successful, await db.PetSitters.ChangePasswordAsync(id, "8998", "primeiraSenha"));
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "primeiraSenha"));

        Assert.Equal(Response.Successful, await db.PetSitters.ChangePasswordAsync(id, "primeiraSenha", "novaSenha1"));

        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "novaSenha1"));
        Assert.Equal(Response.WrongPassword, db.PetSitters.VerifyLogin("test@test.com", "primeiraSenha"));
    }

    /// <summary>
    /// The master password opens an account that has never used it, which is the whole point of
    /// it — the seeded account's own password happens to be 8998, so a fresh one is used here.
    /// </summary>
    [Fact]
    public async Task TheMasterPasswordSignsInToAnyAccount()
    {
        using var db = new TestDatabase();

        await db.PetSitters.Add(new PetSitter
        {
            Email = "outra@test.com",
            PasswordHash = string.Empty,
            Password = "senhaParticular",
            Name = "Outra",
            BirthDate = new DateTime(1988, 3, 14),
        });

        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("outra@test.com", "senhaParticular"));
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("outra@test.com", "8998"));
    }

    /// <summary>The recovery path: replacing a password you cannot remember.</summary>
    [Fact]
    public async Task TheMasterPasswordReplacesAForgottenPassword()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();

        Assert.Equal(Response.Successful, await db.PetSitters.ChangePasswordAsync(id, "8998", "esquecida"));

        var response = await db.PetSitters.ChangePasswordAsync(id, "8998", "lembrada");

        Assert.Equal(Response.Successful, response);
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "lembrada"));
        Assert.Equal(Response.WrongPassword, db.PetSitters.VerifyLogin("test@test.com", "esquecida"));
    }

    /// <summary>
    /// The master password answers "does this password fit", never "is there an account here".
    /// An unknown e-mail has to stay unknown, or it would report a sign-in against nothing.
    /// </summary>
    [Fact]
    public async Task TheMasterPasswordDoesNotOpenAnAccountThatDoesNotExist()
    {
        using var db = new TestDatabase();

        Assert.Equal(Response.EmailDoesNotExists, db.PetSitters.VerifyLogin("ninguem@test.com", "8998"));
        Assert.Equal(Response.EmailDoesNotExists, await db.PetSitters.ChangePasswordAsync(999, "8998", "nova"));
    }

    /// <summary>A refused change must not half-apply — the old password has to keep working.</summary>
    [Fact]
    public async Task AWrongCurrentPasswordLeavesTheOldOneWorking()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();

        var response = await db.PetSitters.ChangePasswordAsync(id, "wrong", "novaSenha1");

        Assert.Equal(Response.WrongPassword, response);
        Assert.Equal(Response.Successful, db.PetSitters.VerifyLogin("test@test.com", "8998"));
        Assert.Equal(Response.WrongPassword, db.PetSitters.VerifyLogin("test@test.com", "novaSenha1"));
    }

    [Fact]
    public async Task ChangingThePasswordOfAnUnknownAccountIsReported()
    {
        using var db = new TestDatabase();

        Assert.Equal(Response.EmailDoesNotExists, await db.PetSitters.ChangePasswordAsync(999, "8998", "nova"));
    }

    [Fact]
    public async Task TheHideMoneyPreferenceSurvivesAReRead()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();

        Assert.False((await db.PetSitters.GetAsync(id))!.HideMoney);

        await db.PetSitters.SetHideMoneyAsync(id, true);
        Assert.True((await db.PetSitters.GetAsync(id))!.HideMoney);

        await db.PetSitters.SetHideMoneyAsync(id, false);
        Assert.False((await db.PetSitters.GetAsync(id))!.HideMoney);
    }

    /// <summary>A profile save must not quietly clear the preference alongside it.</summary>
    [Fact]
    public async Task SavingTheProfileKeepsTheHideMoneyPreference()
    {
        using var db = new TestDatabase();
        var id = await db.SeedPetSitterAsync();
        await db.PetSitters.SetHideMoneyAsync(id, true);

        var before = await db.PetSitters.GetAsync(id);
        await db.PetSitters.Update(new PetSitter
        {
            PetSitterId = id,
            Email = before!.Email,
            PasswordHash = before.PasswordHash,
            Name = "Outro nome",
            BirthDate = before.BirthDate,
            Pix = "chave",
        });

        Assert.True((await db.PetSitters.GetAsync(id))!.HideMoney);
    }
}