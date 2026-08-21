using DapperDemo.Repository.Dapper.Services;

namespace DapperDemo.Viewmodel.Services;

/// <summary>
/// Puts a <see cref="DisplayPreferences"/> into effect: the palette, and the size of the type.
/// </summary>
/// <remarks>
/// An abstraction for the same reason <see cref="ImagePicker"/> is one — applying either means
/// writing to the running Avalonia application, which only the View layer may touch. Not
/// I-prefixed, matching the framework's convention.
/// </remarks>
public interface DisplaySettings
{
    /// <summary>
    /// Gets the step the operating system's own text size corresponds to.
    /// </summary>
    /// <remarks>
    /// Read out by Ajustes while <c>Seguir o tamanho do sistema</c> is on, so the inert slider
    /// still shows where the ramp is sitting rather than showing nothing.
    /// </remarks>
    int SystemTextSizeStep { get; }

    /// <summary>Applies the preference to the running app.</summary>
    /// <param name="preferences">What to apply.</param>
    void Apply(DisplayPreferences preferences);
}