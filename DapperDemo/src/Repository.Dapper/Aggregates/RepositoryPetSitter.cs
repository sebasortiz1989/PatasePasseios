using Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Aggregates;

public sealed class RepositoryPetSitter : RepositoryBase<PetSitter>
{
    public RepositoryPetSitter(DapperDatabaseService dapperDatabaseService)
        : base(dapperDatabaseService)
    {
    }

    public override async Task<Response> Add(PetSitter petSitter)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(petSitter.Password);
            await connection.ExecuteAsync(
                sql: "INSERT INTO PetSitter (Email, PasswordHash, Name, BirthDate, Image) VALUES (@Email, @PasswordHash, @Name, @BirthDate, @Image)",
                param: new { petSitter.Email, PasswordHash = hashedPassword, petSitter.Name, petSitter.BirthDate, petSitter.Image }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return e.Message == "SQLite Error 19: 'UNIQUE constraint failed: PetSitter.Email'." ? Response.EmailExists : Response.Failed;
        }
    }

    public override Task<Response> AddMany(PetSitter petSitter)
    {
        throw new NotImplementedException();
    }

    public override Task<PetSitter> Get(int entityId)
    {
        throw new NotImplementedException();
    }

    public override void GetAll(Action<PetSitter[]> onComplete, Action<Exception>? onError = null)
    {
        Task.Run(async () =>
        {
            try
            {
                using (var connection = DapperDatabaseService.Connection)
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    var petSitters = await connection.QueryAsync<PetSitter>("SELECT * FROM PetSitter").ConfigureAwait(false);
                    var petSitterModelos = petSitters as PetSitter[] ?? petSitters.ToArray();
                    var result = petSitterModelos.Length != 0 ?
                        petSitterModelos.Select(x => new PetSitter
                        {
                            Name = x.Name,
                            BirthDate = x.BirthDate,
                            Email = x.Email,
                            PasswordHash = x.PasswordHash,
                            PetSitterId = x.PetSitterId,
                        }).ToArray() :
                        [];

                    onComplete(result);
                }
            }
#pragma warning disable CA1031 // The onError callback is this method's only error channel: it runs
            // on a background task with no caller to rethrow to, so narrowing the catch would lose
            // failures silently rather than reporting them.
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
#pragma warning restore CA1031
        });
    }

    /// <summary>
    /// Saves an edit from the profile screen: name, birth date and Pix key.
    /// </summary>
    /// <remarks>
    /// Two columns are deliberately left out. PasswordHash, because changing a password is its
    /// own operation and not a side effect of renaming yourself — see
    /// <see cref="ChangePasswordAsync"/>. And Email, because it is the login: it identifies the
    /// account everywhere the app looks one up, so it is treated as fixed once the account
    /// exists. Passing a different <see cref="PetSitter.Email"/> here changes nothing.
    /// </remarks>
    public override async Task<Response> Update(PetSitter petSitter)
    {
        ArgumentNullException.ThrowIfNull(petSitter);

        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: """
                     UPDATE PetSitter
                     SET Name = @Name, BirthDate = @BirthDate, Pix = @Pix, Image = @Image
                     WHERE PetSitterId = @PetSitterId
                     """,
                param: new { petSitter.PetSitterId, petSitter.Name, petSitter.BirthDate, petSitter.Pix, petSitter.Image }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Replaces the account's password, after checking the one it is replacing.
    /// </summary>
    /// <remarks>
    /// The current password is required even though the caller is already signed in: the app has
    /// no lock of its own, so without it anyone holding the unlocked phone could take the account
    /// over. Verifying and rehashing both happen here so BCrypt stays behind the repository, the
    /// same way <see cref="Add"/> and <see cref="VerifyLogin"/> keep it.
    /// </remarks>
    /// <param name="petSitterId">The account to change.</param>
    /// <param name="currentPassword">The password as it stands, to prove the change is wanted.</param>
    /// <param name="newPassword">The replacement, in plain text; only its hash is stored.</param>
    /// <returns>
    /// <see cref="Response.WrongPassword"/> if the current password does not match,
    /// <see cref="Response.EmailDoesNotExists"/> if the account is gone, otherwise the result of the write.
    /// </returns>
    public async Task<Response> ChangePasswordAsync(int petSitterId, string currentPassword, string newPassword)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);

            var storedHash = await connection.QueryFirstOrDefaultAsync<string>(
                sql: "SELECT PasswordHash FROM PetSitter WHERE PetSitterId = @PetSitterId",
                param: new { PetSitterId = petSitterId }).ConfigureAwait(false);

            if (string.IsNullOrEmpty(storedHash))
            {
                return Response.EmailDoesNotExists;
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
            {
                return Response.WrongPassword;
            }

            await connection.ExecuteAsync(
                sql: "UPDATE PetSitter SET PasswordHash = @PasswordHash WHERE PetSitterId = @PetSitterId",
                param: new { PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword), PetSitterId = petSitterId }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Remembers whether the billing figures are bulleted out on screen.
    /// </summary>
    /// <remarks>
    /// A one-column write of its own rather than part of <see cref="Update"/>: the eye is toggled
    /// straight from the reading view, with no editor open and no other field to save alongside
    /// it. Same shape as RepositoryServices.SetPaidAsync.
    /// </remarks>
    /// <param name="petSitterId">The account whose preference this is.</param>
    /// <param name="hide">True to bullet the amounts out, false to show them.</param>
    /// <returns>Whether the write succeeded.</returns>
    public async Task<Response> SetHideMoneyAsync(int petSitterId, bool hide)
    {
        try
        {
            using var connection = DapperDatabaseService.Connection;
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(
                sql: "UPDATE PetSitter SET HideMoney = @Hide WHERE PetSitterId = @PetSitterId",
                param: new { Hide = hide, PetSitterId = petSitterId }).ConfigureAwait(false);

            return Response.Successful;
        }
        catch (SqliteException e)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>The logged-in account's own row, for the profile screen.</summary>
    public async Task<PetSitter?> GetAsync(int petSitterId)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<PetSitter>(
            sql: "SELECT PetSitterId, Email, PasswordHash, Name, BirthDate, Pix, HideMoney, Image FROM PetSitter WHERE PetSitterId = @PetSitterId",
            param: new { PetSitterId = petSitterId }).ConfigureAwait(false);
    }

    public override Task<Response> Delete(int entityId)
    {
        throw new NotImplementedException();
    }

    public override Task<Response> DeleteAll()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The logged-in row, so the app can scope data to this account and greet the user by name.
    /// <see cref="VerifyLogin"/> only reports success, which isn't enough on its own.
    /// </summary>
    public async Task<PetSitter?> GetByEmailAsync(string email)
    {
        using var connection = DapperDatabaseService.Connection;
        await connection.OpenAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<PetSitter>(
            sql: "SELECT PetSitterId, Email, PasswordHash, Name, BirthDate, Pix, HideMoney, Image FROM PetSitter WHERE Email = @Email",
            param: new { Email = email }).ConfigureAwait(false);
    }

    public Response VerifyLogin(string email, string password)
    {
        using (var connection = DapperDatabaseService.Connection)
        {
            connection.Open();
            var petSitter = connection.QueryFirstOrDefault<PetSitter>("SELECT * FROM PetSitter WHERE Email = @Email", new { Email = email });

            if (petSitter is { PasswordHash: not null })
            {
                return BCrypt.Net.BCrypt.Verify(password, petSitter.PasswordHash) ? Response.Successful : Response.WrongPassword;
            }

            return Response.EmailDoesNotExists;
        }
    }
}