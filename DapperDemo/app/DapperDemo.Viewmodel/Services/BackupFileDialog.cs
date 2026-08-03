namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// Asks the user where a backup should go, and which one to read back.
/// </summary>
/// <remarks>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one: both dialogs need a
/// TopLevel, which only the View layer can reach. On Android these map to the Storage Access
/// Framework, so the user picks the destination themselves and the app needs no storage
/// permission. Not I-prefixed, matching the framework's convention.
/// </remarks>
public interface BackupFileDialog
{
    /// <summary>
    /// Asks where to write a new backup.
    /// </summary>
    /// <param name="suggestedName">File name to offer, e.g. patas-backup-2026-08-03.zip.</param>
    /// <returns>A writable stream the caller disposes, or null if the user cancelled.</returns>
    Task<Stream?> CreateAsync(string suggestedName);

    /// <summary>
    /// Asks which backup to restore.
    /// </summary>
    /// <returns>A readable stream the caller disposes, or null if the user cancelled.</returns>
    Task<Stream?> OpenAsync();
}