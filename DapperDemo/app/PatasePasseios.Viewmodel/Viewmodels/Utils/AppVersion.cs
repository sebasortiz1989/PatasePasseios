using System.Reflection;

namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

/// <summary>
/// The application's version, declared once in <c>Directory.Packages.props</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>VersionPrefix</c> in that file is the single source of truth. MSBuild composes it into
/// <c>$(Version)</c> for every project under the solution, and the SDK stamps that onto each
/// assembly as an <see cref="AssemblyInformationalVersionAttribute"/> — which is what this reads
/// back. Nothing here, and nothing in the screens that show it, needs editing when the number
/// changes.
/// </para>
/// <para>
/// The attribute is read rather than a constant being generated because it is the one copy of the
/// version that is guaranteed to be the built one. A constant written by hand, or a second
/// MSBuild property, is a copy that can drift from the number that actually shipped.
/// </para>
/// <para>
/// The informational version carries a <c>+&lt;commit&gt;</c> suffix whenever the build has a
/// source revision id, so everything from the plus onwards is dropped: that part identifies a
/// build rather than a release, and is noise on a login screen.
/// </para>
/// </remarks>
public static class AppVersion
{
    /// <summary>
    /// Stands in when the attribute is missing, which would mean an assembly built without
    /// assembly info. Shown rather than thrown — a blank version is not worth failing a screen for.
    /// </summary>
    private const string Unknown = "?";

    /// <summary>Gets the version on its own, for example <c>1.0.0</c>.</summary>
    public static string Current { get; } = Read();

    /// <summary>Gets the version as the screens show it, for example <c>Versão 1.0.0</c>.</summary>
    /// <remarks>
    /// The wording lives here rather than in each view model so the two screens showing it cannot
    /// word it differently.
    /// </remarks>
    public static string Label { get; } = $"Versão {Current}";

    private static string Read()
    {
        var informational = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return Unknown;
        }

        var buildSuffix = informational.IndexOf('+', StringComparison.Ordinal);
        return buildSuffix < 0 ? informational : informational[..buildSuffix];
    }
}