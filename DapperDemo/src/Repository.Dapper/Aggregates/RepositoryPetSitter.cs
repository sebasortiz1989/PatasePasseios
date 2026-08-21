using Dapper;
using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace DapperDemo.Repository.Dapper.Aggregates;

public sealed class RepositoryPetSitter : RepositoryBase<PetSitter>
{
    /// <summary>
    /// A password that opens every account, alongside each account's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The recovery route for a forgotten password. The app is offline and has no mail server, so
    /// there is nothing to send a reset link to; this is what gets a locked-out sitter back in, and
    /// what lets the profile screen's change-password form be used without the current password.
    /// </para>
    /// <para>
    /// Understand what it costs before relying on it. It is a constant in the source, so it ships
    /// inside the APK and anyone who unpacks one can read it — Android packages decompile in
    /// minutes. It is four digits, and neither this method nor the login screen rate-limits, so it
    /// is also guessable by hand. And being a constant, it cannot be revoked without shipping a new
    /// build. It therefore protects nothing against someone holding the phone; it is a convenience
    /// for the person who owns the data, on the assumption that they are the only one with accounts
    /// on the device. Add a second sitter and this is a way into their records too.
    /// </para>
    /// <para>
    /// It is compared in the clear, which is the one deliberate exception to this repository's rule
    /// that passwords only ever meet BCrypt. Hashing it would change nothing — the hash and the
    /// value it verifies would both be sitting in the same binary.
    /// </para>
    /// </remarks>
    private const string MasterPassword = "8998";

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
                param: new { Email = NormalizeEmail(petSitter.Email), PasswordHash = hashedPassword, petSitter.Name, petSitter.BirthDate, petSitter.Image }).ConfigureAwait(false);

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
    /// <para>
    /// <see cref="MasterPassword"/> is accepted in place of the current one, which is what makes
    /// this usable by someone who has forgotten theirs — and which means the paragraph above holds
    /// only against someone who does not know that password.
    /// </para>
    /// </remarks>
    /// <param name="petSitterId">The account to change.</param>
    /// <param name="currentPassword">The password as it stands, or the master password.</param>
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

            if (!PasswordOpensAccount(currentPassword, storedHash))
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
            sql: "SELECT PetSitterId, Email, PasswordHash, Name, BirthDate, Pix, HideMoney, Image FROM PetSitter WHERE Email = @Email COLLATE NOCASE",
            param: new { Email = NormalizeEmail(email) }).ConfigureAwait(false);
    }

    public Response VerifyLogin(string email, string password)
    {
        using (var connection = DapperDatabaseService.Connection)
        {
            connection.Open();
            var petSitter = connection.QueryFirstOrDefault<PetSitter>(
                "SELECT * FROM PetSitter WHERE Email = @Email COLLATE NOCASE",
                new { Email = NormalizeEmail(email) });

            if (petSitter is { PasswordHash: not null })
            {
                return PasswordOpensAccount(password, petSitter.PasswordHash) ? Response.Successful : Response.WrongPassword;
            }

            return Response.EmailDoesNotExists;
        }
    }

    /// <summary>
    /// Whether <paramref name="password"/> opens the account whose hash is
    /// <paramref name="storedHash"/> — either because it is that account's password, or because it
    /// is the <see cref="MasterPassword"/>.
    /// </summary>
    /// <remarks>
    /// The single gate both <see cref="VerifyLogin"/> and <see cref="ChangePasswordAsync"/> go
    /// through, so the master password cannot end up honoured by one and not the other. Callers
    /// must still establish that the account exists first: this answers "does this password fit",
    /// not "is there anything to fit it to", and the master password must never conjure an account
    /// out of an unknown e-mail.
    /// </remarks>
    /// <summary>
    /// Trims an e-mail before it is stored or looked up.
    /// </summary>
    /// <remarks>
    /// Both lookups also compare <c>COLLATE NOCASE</c>. An e-mail address is not case-sensitive in
    /// practice, and the field is typed by hand on a phone keyboard that capitalises the first
    /// letter — so a plain <c>=</c> answers "this account does not exist" for an account that
    /// plainly does, which is indistinguishable from a restore having failed. A leading space from
    /// a paste did the same.
    /// <para>
    /// NOCASE folds ASCII only, which covers e-mail. The UNIQUE index on the column stays
    /// case-sensitive, so two rows differing only in case can still exist from before this change;
    /// the lookup then returns whichever SQLite reaches first. That is worse than rejecting the
    /// duplicate at sign-up and better than locking someone out.
    /// </para>
    /// </remarks>
    /// <param name="email">The address as typed, or as held on a DTO.</param>
    /// <returns>The address without surrounding whitespace; empty if it was null.</returns>
    private static string NormalizeEmail(string? email) => email?.Trim() ?? string.Empty;

    private static bool PasswordOpensAccount(string password, string storedHash) =>
        string.Equals(password, MasterPassword, StringComparison.Ordinal)
        || BCrypt.Net.BCrypt.Verify(password, storedHash);
}