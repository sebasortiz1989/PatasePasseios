using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DapperDemo.Viewmodel.Services;
using System.IO;
using System.Threading.Tasks;

namespace DapperDemo.View.Services;

/// <summary>
/// Save and open dialogs for the backup file, through Avalonia's storage provider — the Storage
/// Access Framework on Android, the native file dialog elsewhere.
/// </summary>
public sealed class StorageProviderBackupFileDialog : BackupFileDialog
{
    /// <summary>
    /// Zip is declared by MIME type as well as by extension: Android's picker filters on the MIME
    /// type, and a type it does not recognise leaves the file greyed out in Drive and Downloads.
    /// </summary>
    private static readonly FilePickerFileType ZipFileType = new("Backup (.zip)")
    {
        Patterns = ["*.zip"],
        MimeTypes = ["application/zip"],
        AppleUniformTypeIdentifiers = ["public.zip-archive"],
    };

    /// <inheritdoc/>
    public async Task<Stream?> CreateAsync(string suggestedName)
    {
        var storageProvider = GetTopLevel()?.StorageProvider;
        if (storageProvider is not { CanSave: true })
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar backup",
            SuggestedFileName = suggestedName,
            DefaultExtension = "zip",
            FileTypeChoices = [ZipFileType],
            ShowOverwritePrompt = true,
        }).ConfigureAwait(true);

        if (file == null)
        {
            return null;
        }

        return await file.OpenWriteAsync().ConfigureAwait(true);
    }

    /// <inheritdoc/>
    public async Task<Stream?> OpenAsync()
    {
        var storageProvider = GetTopLevel()?.StorageProvider;
        if (storageProvider is not { CanOpen: true })
        {
            return null;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar backup",
            AllowMultiple = false,
            FileTypeFilter = [ZipFileType],
        }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return null;
        }

        return await files[0].OpenReadAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// The window (desktop) or root view (mobile) the dialogs hang off — the same lookup
    /// <see cref="StorageProviderImagePicker"/> uses.
    /// </summary>
    private static TopLevel? GetTopLevel() => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
        _ => null,
    };
}
