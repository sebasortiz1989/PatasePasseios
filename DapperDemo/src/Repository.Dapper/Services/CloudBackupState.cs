using System.Globalization;
using System.Text.Json;

namespace PatasePasseios.Repository.Dapper.Services;

/// <summary>
/// Reads and writes the <see cref="CloudBackupSchedule"/> this device keeps on disk.
/// </summary>
/// <remarks>
/// <para>
/// A file beside the database rather than a column on PetSitter, for two reasons. The question it
/// answers is "when did <i>this device</i> last upload", so a restore carrying another device's
/// answer would get it wrong and skip a backup that is actually overdue. And it is read right
/// after login, which is not a good moment to depend on a particular sitter's row.
/// </para>
/// <para>
/// Reads are forgiving: missing, empty or unparseable all come back as
/// <see cref="CloudBackupSchedule.Empty"/>, which schedules a backup rather than suppressing one.
/// Losing this file must never be the reason backups quietly stop.
/// </para>
/// </remarks>
public sealed class CloudBackupState
{
    private const string FileName = "cloud-backup.json";

    private readonly string path;

    /// <summary>Initializes a new instance of the <see cref="CloudBackupState"/> class.</summary>
    public CloudBackupState()
        : this(Path.Combine(AppStorage.Folder, FileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudBackupState"/> class over a given file.
    /// </summary>
    /// <remarks>
    /// The parameterless constructor is what the app uses. This one exists so the tests can drive
    /// the real read and write against a throwaway file instead of the state belonging to whoever
    /// is running them.
    /// </remarks>
    /// <param name="filePath">Where the state file lives. Created on first write.</param>
    public CloudBackupState(string filePath) => path = filePath;

    /// <summary>Reads the stored schedule.</summary>
    /// <returns>The schedule, or <see cref="CloudBackupSchedule.Empty"/> if none is readable.</returns>
    public async Task<CloudBackupSchedule> ReadAsync()
    {
        try
        {
            if (!File.Exists(path))
            {
                return CloudBackupSchedule.Empty;
            }

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            // JsonDocument rather than a deserialized type: this assembly is compiled ahead of
            // time for the iOS head, where reflection-based deserialization is the thing that
            // works in Debug and fails once trimmed.
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var destination = root.TryGetProperty("destination", out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

            return new CloudBackupSchedule(
                ReadStamp(root, "lastUploadUtc"),
                ReadStamp(root, "lastAttemptUtc"),
                destination,
                ReadStamp(root, "lastPromptUtc"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.WriteLine(e);
            return CloudBackupSchedule.Empty;
        }
    }

    /// <summary>Stores the schedule, replacing whatever was there.</summary>
    /// <param name="schedule">What to store.</param>
    /// <returns>Successful, or Failed when the file could not be written.</returns>
    public async Task<Response> WriteAsync(CloudBackupSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        try
        {
            var json =
                $$"""
                {
                  "lastUploadUtc": {{Stamp(schedule.LastUploadUtc)}},
                  "lastAttemptUtc": {{Stamp(schedule.LastAttemptUtc)}},
                  "destination": {{Text(schedule.Destination)}},
                  "lastPromptUtc": {{Stamp(schedule.LastPromptUtc)}}
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

    private static DateTime? ReadStamp(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(
            property.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var value)
            ? value.ToUniversalTime()
            : null;
    }

    /// <summary>Writes a string as JSON, or null. Escaped, because a bookmark is opaque bytes.</summary>
    private static string Text(string? value) =>
        value == null ? "null" : JsonSerializer.Serialize(value);

    private static string Stamp(DateTime? value) =>
        value == null ? "null" : $"\"{value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)}\"";
}