using Avalonia;
using Avalonia.Styling;
using PatasePasseios.Repository.Dapper.Services;
using PatasePasseios.Viewmodel.Services;

namespace PatasePasseios.View.Services;

/// <summary>
/// Applies the display preference by writing to the running Avalonia application.
/// </summary>
/// <remarks>
/// The type sizes live in <c>Application.Resources</c> rather than in ClassicalTheme's own
/// dictionary so there is exactly one place to write them, with no question of which lookup wins.
/// Every view binds them with <c>DynamicResource</c>, so reassigning one here re-measures the text
/// wherever it is used without any screen knowing the ramp moved.
/// </remarks>
public sealed class AvaloniaDisplaySettings : DisplaySettings
{
    /// <inheritdoc/>
    /// <remarks>
    /// Avalonia exposes no portable read of the operating system's text scale, so this reports the
    /// default step. It is the seam the platform value belongs in — iOS Dynamic Type and Android's
    /// font scale both have one — and until it is wired, <c>Seguir o tamanho do sistema</c> holds
    /// the ramp at Padrão rather than claiming a size the app cannot see.
    /// </remarks>
    public int SystemTextSizeStep => DisplayPreferences.DefaultStep;

    /// <inheritdoc/>
    public void Apply(DisplayPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        if (Application.Current is not Application application)
        {
            return;
        }

        application.RequestedThemeVariant = preferences.Theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        // Following the system means the app's own step is ignored rather than overwritten, so
        // turning the switch back off returns to the size the user last chose.
        var step = TextSizeRamp.At(
            preferences.FollowSystemTextSize ? SystemTextSizeStep : preferences.TextSizeStep);

        var resources = application.Resources;
        resources["TypeDisplay"] = step.Display;
        resources["TypeTitle"] = step.Title;
        resources["TypeSection"] = step.Section;
        resources["TypeBody"] = step.Body;
        resources["TypeUi"] = step.Ui;
        resources["TypeCaption"] = step.Caption;
        resources["TypeMicro"] = step.Micro;
    }
}