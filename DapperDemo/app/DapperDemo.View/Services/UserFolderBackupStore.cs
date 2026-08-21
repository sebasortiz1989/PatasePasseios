using Avalonia.Platform.Storage;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;

namespace DapperDemo.View.Services;

/// <summary>
/// Writes automatic backups to a folder the user picked, remembered between launches.
/// </summary>
/// <remarks>
/// <para>
/// The stand-in for the Google Drive store, and a usable destination in its own right — unlike the
/// app's private folder, which is where this first wrote. Private storage is invisible in the Files
/// app, unreachable by anything else, and deleted with the app on uninstall: a backup that dies
/// with the app is not a backup.
/// </para>
/// <para>
/// The folder is kept as an Avalonia storage bookmark rather than a path. A path is meaningless on
/// Android — the picker returns a <c>content://</c> tree URI — and the bookmark is also what
/// carries the SAF permission grant forward, so a later launch can write there without asking
/// again.
/// </para>
/// </remarks>
public sealed class UserFolderBackupStore(CloudBackupState state) : CloudBackupStore
{
    /// <inheritdoc/>
    public string DisplayName => "pasta escolhida";

    private CloudBackupState State { get; } = state;

    /// <inheritdoc/>
    public async Task<bool> IsLinkedAsync() => await OpenDestinationAsync().WithSync() != null;

    /// <inheritdoc/>
    public async Task<Response> LinkAsync()
    {
        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
        if (storageProvider is not { CanPickFolder: true })
        {
            return Response.Failed;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Onde guardar os backups",
            AllowMultiple = false,
        }).WithSync();

        if (folders.Count == 0)
        {
            return Response.Failed;
        }

        // A bookmark is what survives the process; the IStorageFolder itself does not.
        var bookmark = await folders[0].SaveBookmarkAsync().WithSync();
        if (string.IsNullOrWhiteSpace(bookmark))
        {
            return Response.Failed;
        }

        var schedule = await State.ReadAsync().WithSync();
        return await State.WriteAsync(schedule with { Destination = bookmark }).WithSync();
    }

    /// <inheritdoc/>
    public async Task<Response> UploadAsync(Stream content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);

        var folder = await OpenDestinationAsync().WithSync();
        if (folder == null)
        {
            return Response.Failed;
        }

        try
        {
            // Only the bare name is used, so a caller cannot walk out of the folder.
            var name = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(name))
            {
                return Response.Failed;
            }

            // CreateFileAsync returns the existing file truncated rather than inventing
            // "patas-backup (1).zip", which is what makes this overwrite in place and keep the
            // name exact — the same behaviour the export dialog relies on for Android.
            var file = await folder.CreateFileAsync(name).WithSync();
            if (file == null)
            {
                return Response.Failed;
            }

            var target = await file.OpenWriteAsync().WithSync();
            await using (target.ConfigureAwait(false))
            {
                await content.CopyToAsync(target).NoSync();
            }

            return Response.Successful;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Resolves the remembered folder, or null when there is none or it is no longer reachable.
    /// </summary>
    private async Task<IStorageFolder?> OpenDestinationAsync()
    {
        var schedule = await State.ReadAsync().WithSync();
        if (schedule.Destination is not { Length: > 0 } bookmark)
        {
            return null;
        }

        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
        if (storageProvider == null)
        {
            return null;
        }

        try
        {
            // Null when the folder is gone, the card was removed, or the grant was revoked. The
            // caller treats that as "not linked", which sends the user back to the picker rather
            // than failing a backup they thought was configured.
            return await storageProvider.TryGetFolderFromPathAsync(bookmark).WithSync()
                ?? await storageProvider.OpenFolderBookmarkAsync(bookmark).WithSync();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}