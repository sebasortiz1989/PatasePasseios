namespace DapperDemo.Repository.Dapper.Services;

/// <summary>
/// Where a dog's photo lives on disk.
/// </summary>
/// <remarks>
/// The Dogs table's Image column holds a bare file name, not the bytes: a photo straight off a
/// phone camera is a few megabytes, and the dogs list reads every row at once, so storing blobs
/// would pull the whole gallery through SQLite on each refresh. Keeping only the name also means
/// the folder can move between platforms without rewriting rows.
/// </remarks>
public static class DogImageStore
{
    private const string FolderName = "DogImages";

    /// <summary>Gets the folder holding every stored dog photo.</summary>
    public static string Folder => AppStorage.SubFolder(FolderName);

    /// <summary>
    /// Copies a picked image into the store under a fresh name.
    /// </summary>
    /// <param name="content">The picked file's contents. Not disposed here.</param>
    /// <param name="extension">File extension including the dot, e.g. <c>.jpg</c>.</param>
    /// <returns>The stored file name, to be written to the dog's Image column.</returns>
    public static async Task<string> SaveAsync(Stream content, string extension)
    {
        ArgumentNullException.ThrowIfNull(content);

        // A new name per save rather than one keyed on the dog: replacing a photo must not
        // overwrite a file the previous Bitmap may still have open for rendering.
        var fileName = Guid.NewGuid().ToString("N") + NormalizeExtension(extension);
        var path = Path.Combine(Folder, fileName);

        var target = File.Create(path);

#pragma warning disable SA1312
        await using var _ = target.ConfigureAwait(false);
#pragma warning restore SA1312
        await content.CopyToAsync(target).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// Turns a stored file name into a full path the UI can load.
    /// </summary>
    /// <param name="fileName">The dog's Image column, which may be null or stale.</param>
    /// <returns>The absolute path, or null when there is no photo or the file has gone missing.</returns>
    public static string? ResolvePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Guards against a row pointing at a file the user deleted underneath the app, which
        // would otherwise surface as an unhandled load failure inside a data template.
        var path = Path.Combine(Folder, fileName);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Removes a stored photo. Missing files are ignored — the goal is that it is gone.</summary>
    /// <param name="fileName">The stored file name, or null to do nothing.</param>
    public static void Delete(string? fileName)
    {
        var path = ResolvePath(fileName);
        if (path == null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException e)
        {
            // A photo still open for rendering cannot be deleted on Windows. Leaving the file
            // behind is harmless: the row no longer points at it.
            Console.WriteLine(e);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine(e);
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".img";
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return normalized.ToLowerInvariant();
    }
}