using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;

namespace DapperDemo.Infrastructure.Services;

/// <summary>
/// Writes automatic backups to a folder on this device.
/// </summary>
/// <remarks>
/// <para>
/// The stand-in for the Google Drive store, which cannot be written until there is an OAuth client
/// id to sign in against. It occupies the same slot and satisfies the same abstraction, so
/// everything around it — the schedule, the prompt, the wiring, the failure paths — is exercised
/// and testable now, and the swap is one registration.
/// </para>
/// <para>
/// It lives here rather than in the View layer, where the other implementations of Viewmodel
/// abstractions sit, because it needs <see cref="AppStorage"/> and the View layer deliberately
/// does not reference the data layer. The Drive store will need a TopLevel to open a browser, so
/// that one does belong in View.
/// </para>
/// </remarks>
public sealed class LocalFolderBackupStore : CloudBackupStore
{
    private const string FolderName = "CloudBackup";

    /// <summary>Gets the folder backups are written to, created on first use.</summary>
    public static string Folder => AppStorage.SubFolder(FolderName);

    /// <inheritdoc/>
    public string DisplayName => "pasta local deste aparelho";

    /// <inheritdoc/>
    public async Task<Response> UploadAsync(Stream content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            // Only the bare name is used, so a caller cannot walk out of the folder with a name
            // like ../../something.
            var name = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(name))
            {
                return Response.Failed;
            }

            // Written beside the target and moved into place, so a run that dies halfway cannot
            // leave a truncated file sitting where the last good backup used to be.
            var destination = Path.Combine(Folder, name);
            var staging = destination + ".part";

            var file = File.Create(staging);
            await using (file.ConfigureAwait(false))
            {
                await content.CopyToAsync(file).ConfigureAwait(false);
            }

            File.Move(staging, destination, overwrite: true);
            return Response.Successful;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }
}