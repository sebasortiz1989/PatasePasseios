using Avalonia.Platform.Storage;
using AvaloniaFramework.Imaging;
using PatasePasseios.Viewmodel.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PatasePasseios.View.Services;

/// <summary>
/// Picks an image through Avalonia's storage provider — the native file browser on every head.
/// </summary>
public sealed class StorageProviderImagePicker : ImagePicker
{
    /// <inheritdoc/>
    public async Task<PickedImage?> PickAsync()
    {
        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
        if (storageProvider is not { CanOpen: true })
        {
            return null;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolher foto",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return null;
        }

        var file = files[0];

        // Android returns a content:// URI whose Name still carries the original extension;
        // falling back to .jpg only matters for a file the picker could not name at all.
        var extension = Path.GetExtension(file.Name);
        extension = string.IsNullOrEmpty(extension) ? ".jpg" : extension;

        var stream = await file.OpenReadAsync().ConfigureAwait(true);

        // Reduced here rather than in the image store, which is in the data layer and has no
        // codecs. Every caller of the picker gets it — dog photos and profile photos alike — and
        // what reaches disk and the backup zip is already the size the app draws.
        await using (stream.ConfigureAwait(true))
        {
            var (content, storedExtension) = await PhotoDownscaler.ReduceAsync(stream, extension).ConfigureAwait(true);
            return new PickedImage(content, storedExtension);
        }
    }
}