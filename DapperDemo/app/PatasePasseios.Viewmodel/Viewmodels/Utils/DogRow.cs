using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

public sealed class DogRow(string initials, string name, string subtitle, string? imagePath, ICommand openCommand) : IDisposable
{
    public string Initials { get; } = initials;

    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    /// <summary>Gets the dog's photo on disk, or null when it has none.</summary>
    public string? ImagePath { get; } = imagePath;

    /// <summary>Gets a value indicating whether the row shows a photo rather than the initials.</summary>
    public bool HasImage => ImagePath != null;

    public bool NoImage => ImagePath == null;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}