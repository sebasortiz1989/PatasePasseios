using System.Globalization;
using System.Text.Json;

namespace DapperDemo.Repository.Dapper.Services;

/// <summary>
/// Reads and writes the <see cref="DisplayPreferences"/> this device is using.
/// </summary>
/// <remarks>
/// <para>
/// A file beside the database rather than a column on PetSitter, for the same reason
/// <see cref="CloudBackupState"/> is one: how big the type is and which palette is drawn belong to
/// the phone, not to the account. Restoring someone else's backup must not change either, and the
/// preference has to be readable before anyone has signed in — the login screen is drawn in it.
/// </para>
/// <para>
/// Reads are forgiving: missing, empty or unparseable all come back as
/// <see cref="DisplayPreferences.Default"/>, so a damaged file means the app opens looking ordinary
/// rather than not opening.
/// </para>
/// </remarks>
public sealed class DisplayPreferencesStore
{
    private const string FileName = "display.json";

    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="DisplayPreferencesStore"/> class.</summary>
    public DisplayPreferencesStore()
        : this(Path.Combine(AppStorage.Folder, FileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayPreferencesStore"/> class over a given file.
    /// </summary>
    /// <remarks>
    /// The parameterless constructor is what the app uses. This one exists so the tests can drive
    /// the real read and write against a throwaway file.
    /// </remarks>
    /// <param name="filePath">Where the preference file lives. Created on first write.</param>
    public DisplayPreferencesStore(string filePath) => path = filePath;

    /// <summary>
    /// Reads the stored preference, synchronously.
    /// </summary>
    /// <remarks>
    /// For app startup, which cannot await. <c>OnFrameworkInitializationCompleted</c> has to set
    /// the desktop lifetime's MainWindow before it returns — the moment it does, Avalonia starts
    /// the main loop — so awaiting anything before the window is built hands control back with no
    /// window yet and the app comes up blank. It did not always: reading a small cached file often
    /// completes without yielding, so the await version worked until it did not, which is the worst
    /// kind of intermittent.
    /// <para>
    /// Blocking is safe here and only here: one small local file, on a thread that has nothing to
    /// render yet.
    /// </para>
    /// </remarks>
    /// <returns>The preference, or <see cref="DisplayPreferences.Default"/> if none is readable.</returns>
    public DisplayPreferences Read()
    {
        try
        {
            return File.Exists(path) ? Parse(File.ReadAllText(path)) : DisplayPreferences.Default;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.WriteLine(e);
            return DisplayPreferences.Default;
        }
    }

    /// <summary>Reads the stored preference.</summary>
    /// <returns>The preference, or <see cref="DisplayPreferences.Default"/> if none is readable.</returns>
    public async Task<DisplayPreferences> ReadAsync()
    {
        try
        {
            if (!File.Exists(path))
            {
                return DisplayPreferences.Default;
            }

            return Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.WriteLine(e);
            return DisplayPreferences.Default;
        }
    }

    /// <summary>Stores the preference, replacing whatever was there.</summary>
    /// <param name="preferences">What to store.</param>
    /// <returns>Successful, or Failed when the file could not be written.</returns>
    public async Task<Response> WriteAsync(DisplayPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        try
        {
            var clamped = preferences.Clamped();
            var json =
                $$"""
                {
                  "theme": "{{clamped.Theme}}",
                  "textSizeStep": {{clamped.TextSizeStep.ToString(CultureInfo.InvariantCulture)}},
                  "followSystemTextSize": {{(clamped.FollowSystemTextSize ? "true" : "false")}}
                }
                """;

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            return Response.Successful;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Response.Failed;
        }
    }

    /// <summary>
    /// Turns the file's contents into a preference.
    /// </summary>
    /// <remarks>
    /// JsonDocument rather than a deserialized type: this assembly is compiled ahead of time for
    /// the iOS head, where reflection-based deserialization works in Debug and fails once trimmed.
    /// </remarks>
    /// <param name="json">The file's contents.</param>
    /// <returns>The preference it describes, with the step forced into range.</returns>
    private static DisplayPreferences Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var theme = root.TryGetProperty("theme", out var themeValue)
            && Enum.TryParse<AppTheme>(themeValue.GetString(), ignoreCase: true, out var parsed)
            ? parsed
            : DisplayPreferences.Default.Theme;

        var step = root.TryGetProperty("textSizeStep", out var stepValue) && stepValue.TryGetInt32(out var number)
            ? number
            : DisplayPreferences.DefaultStep;

        var follow = !root.TryGetProperty("followSystemTextSize", out var followValue)
            || followValue.ValueKind != JsonValueKind.False;

        return new DisplayPreferences(theme, step, follow).Clamped();
    }
}