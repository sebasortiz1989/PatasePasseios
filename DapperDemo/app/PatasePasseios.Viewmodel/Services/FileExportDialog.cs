namespace PatasePasseios.Viewmodel.Services;

/// <summary>What is being written, which decides the extension and the picker's file type.</summary>
public enum ExportFileKind
{
    /// <summary>A report image.</summary>
    Png,

    /// <summary>A backup archive.</summary>
    Zip,
}

/// <summary>
/// Asks the user where an exported file should go, and which backup to read back.
/// </summary>
/// <remarks>
/// <para>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one: these dialogs need a
/// TopLevel, which only the View layer can reach. Not I-prefixed, matching the framework's
/// convention.
/// </para>
/// <para>
/// It takes a confirmation callback rather than answering the overwrite question itself, because
/// the answer comes from a dialog bound to a view model and the View layer cannot reach one.
/// </para>
/// </remarks>
public interface FileExportDialog
{
    /// <summary>
    /// Asks where to write, and opens the destination.
    /// </summary>
    /// <param name="baseName">File name without extension, e.g. <c>faturamento-2026-08</c>.</param>
    /// <param name="kind">What is being written; supplies the extension.</param>
    /// <param name="confirmReplace">
    /// Asked with the file name when something is already there. Returning false cancels the
    /// export. Only called on platforms where this class does the naming itself — elsewhere the
    /// system's own save dialog asks.
    /// </param>
    /// <returns>The opened destination, or null if the user cancelled.</returns>
    Task<ExportTarget?> CreateAsync(string baseName, ExportFileKind kind, Func<string, Task<bool>> confirmReplace);

    /// <summary>
    /// Asks which backup to restore.
    /// </summary>
    /// <returns>A readable stream the caller disposes, or null if the user cancelled.</returns>
    Task<Stream?> OpenBackupAsync();
}

/// <summary>An opened destination: where to write, and the name the file ended up with.</summary>
/// <param name="Content">The stream to write to. The caller owns and disposes it.</param>
/// <param name="Name">The file name including extension, for reporting back to the user.</param>
public sealed record ExportTarget(Stream Content, string Name);