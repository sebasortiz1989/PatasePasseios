namespace PatasePasseios.Viewmodel.Services;

/// <summary>
/// An image the user chose, handed over as a stream rather than a path.
/// </summary>
/// <remarks>
/// Android hands back a content:// URI with no file system path behind it, so a picker that
/// returned a path would work on desktop and quietly fail on a phone.
/// </remarks>
/// <param name="content">The file's contents, owned by this instance.</param>
/// <param name="extension">The original file extension including the dot, e.g. <c>.jpg</c>.</param>
public sealed class PickedImage(Stream content, string extension) : IDisposable
{
    /// <summary>Gets the chosen file's contents, positioned at the start.</summary>
    public Stream Content { get; } = content;

    /// <summary>Gets the original file extension, including the leading dot.</summary>
    public string Extension { get; } = extension;

    /// <inheritdoc/>
    public void Dispose() => Content.Dispose();
}