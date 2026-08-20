using DapperDemo.Repository.Dapper;

namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// Where an automatic backup is sent.
/// </summary>
/// <remarks>
/// <para>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one: the implementation that
/// matters here talks to a cloud account, and signing into one needs a browser and a TopLevel that
/// only the View layer can reach. Not I-prefixed, matching the framework's convention.
/// </para>
/// <para>
/// There is deliberately no notion of "link this account" yet. The only implementation is
/// <c>LocalFolderBackupStore</c>, which has nothing to sign into; a <c>LinkAsync</c> shaped around
/// an OAuth flow that has not been written would be a guess.
/// </para>
/// </remarks>
public interface CloudBackupStore
{
    /// <summary>Gets the destination's name as the user should see it, already in pt-BR.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Writes the archive to the destination, replacing any file already stored under that name.
    /// </summary>
    /// <param name="content">The archive, readable from its current position. The caller disposes it.</param>
    /// <param name="fileName">The name to store it under, including extension.</param>
    /// <returns>Successful, or Failed when the destination could not be written.</returns>
    Task<Response> UploadAsync(Stream content, string fileName);
}