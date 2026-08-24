namespace PatasePasseios.Repository.Dapper.Services;

/// <summary>
/// The one folder this app may write to, resolved the same way on every platform it runs on.
/// The database file and the dog photos both live under it, so the lookup lives here rather than
/// being duplicated by each of them.
/// </summary>
public static class AppStorage
{
    /// <summary>
    /// Gets the app's private data folder, created on first use.
    /// </summary>
    /// <remarks>
    /// LocalApplicationData is the one folder .NET maps sensibly everywhere the app runs —
    /// AppData\Local on Windows, ~/.local/share on Linux, ~/Library/Application Support on macOS,
    /// and the app's own writable sandbox on iOS and Android. That last part matters: the mobile
    /// heads have no usable current directory (the iOS bundle is read-only), so they must not
    /// fall back to one.
    /// </remarks>
    public static string Folder { get; } = CreateFolder();

    /// <summary>
    /// Returns a subfolder of <see cref="Folder"/>, creating it if it does not exist yet.
    /// </summary>
    /// <param name="name">Folder name, relative to the app data folder.</param>
    /// <returns>The absolute path of the subfolder.</returns>
    public static string SubFolder(string name)
    {
        var path = Path.Combine(Folder, name);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    private static string CreateFolder()
    {
        var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrEmpty(baseFolder))
        {
            baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        if (string.IsNullOrEmpty(baseFolder))
        {
            baseFolder = AppContext.BaseDirectory;
        }

        var folder = Path.Combine(baseFolder, "DapperDemo");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return folder;
    }
}