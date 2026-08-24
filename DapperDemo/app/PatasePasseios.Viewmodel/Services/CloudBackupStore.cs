using PatasePasseios.Repository.Dapper;

namespace PatasePasseios.Viewmodel.Services;

/// <summary>
/// Where an automatic backup is sent, and how the user chooses it.
/// </summary>
/// <remarks>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one: choosing a folder needs a
/// TopLevel, which only the View layer can reach. Not I-prefixed, matching the framework's
/// convention.
/// </remarks>
public interface CloudBackupStore
{
    /// <summary>
    /// The chosen folder's own name, as the file browser shows it.
    /// </summary>
    /// <remarks>
    /// Asynchronous because the destination is stored as a bookmark: naming it means resolving it,
    /// which touches storage. Null when nothing is linked or the folder is no longer reachable —
    /// the caller turns that into the "not set up yet" wording rather than inventing a name.
    /// </remarks>
    /// <returns>The folder's display name, or null.</returns>
    Task<string?> DestinationNameAsync();

    /// <summary>
    /// Whether a destination has been chosen and is still reachable.
    /// </summary>
    /// <remarks>
    /// Reachability is checked, not assumed: a folder can be deleted, an SD card removed, or a
    /// permission grant revoked between one launch and the next.
    /// </remarks>
    /// <returns>True when a backup could be written right now.</returns>
    Task<bool> IsLinkedAsync();

    /// <summary>
    /// Asks the user where backups should go, and remembers it.
    /// </summary>
    /// <returns>Successful, or Failed if they cancelled or the choice could not be kept.</returns>
    Task<Response> LinkAsync();

    /// <summary>
    /// Writes the archive to the chosen destination, replacing the file already there.
    /// </summary>
    /// <param name="content">The archive, readable from its current position. The caller disposes it.</param>
    /// <param name="fileName">The name to store it under, including extension.</param>
    /// <returns>Successful, or Failed when there is no destination or it could not be written.</returns>
    Task<Response> UploadAsync(Stream content, string fileName);
}