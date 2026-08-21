using DapperDemo.Repository.Dapper;

namespace DapperDemo.Viewmodel.Services;

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
    /// <summary>Gets the destination's name as the user should see it, already in pt-BR.</summary>
    string DisplayName { get; }

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