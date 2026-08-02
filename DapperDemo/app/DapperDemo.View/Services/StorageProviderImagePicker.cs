using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DapperDemo.Viewmodel.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DapperDemo.View.Services;

/// <summary>
/// Picks an image through Avalonia's storage provider — the native file browser on every head.
/// </summary>
public sealed class StorageProviderImagePicker : ImagePicker
{
    /// <inheritdoc/>
    public async Task<PickedImage?> PickAsync()
    {
        var storageProvider = GetTopLevel()?.StorageProvider;
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
        var stream = await file.OpenReadAsync().ConfigureAwait(true);

        // Android returns a content:// URI whose Name still carries the original extension;
        // falling back to .jpg only matters for a file the picker could not name at all.
        var extension = Path.GetExtension(file.Name);
        return new PickedImage(stream, string.IsNullOrEmpty(extension) ? ".jpg" : extension);
    }

    /// <summary>
    /// The window (desktop) or root view (mobile) the picker hangs off. Resolved from the
    /// lifetime rather than passed in, so view models can stay free of Avalonia types.
    /// </summary>
    private static TopLevel? GetTopLevel() => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
        _ => null,
    };
}