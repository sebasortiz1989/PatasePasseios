using Avalonia.Platform.Storage;
using PatasePasseios.Viewmodel.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PatasePasseios.View.Services;

/// <summary>
/// Save and open dialogs for exported files, through Avalonia's storage provider.
/// </summary>
/// <remarks>
/// <para>
/// Two routes to the same place, because Android's save dialog cannot be trusted with the file
/// name. Avalonia's Android backend builds the <c>ACTION_CREATE_DOCUMENT</c> intent with a
/// hard-coded <c>*/*</c> MIME type — the types below reach the intent only as a filter — and
/// Android decides where the extension ends by comparing that MIME type against the name it was
/// given. With <c>*/*</c> nothing matches, so a colliding <c>maria.png</c> is treated as one long
/// extensionless name and saved as <c>maria.png (2)</c>, which no viewer will open. Android also
/// has no overwrite prompt to ask for: <c>ShowOverwritePrompt</c> is ignored there, and
/// <c>ACTION_CREATE_DOCUMENT</c> renames rather than asking, by design.
/// </para>
/// <para>
/// So on Android the user picks a *folder* and this class does the naming, which puts both problems
/// in reach: it can see whether the file is already there and ask, and
/// <see cref="IStorageFolder.CreateFileAsync"/> truncates an existing file in place rather than
/// inventing a new name. Everywhere else the system's save dialog already handles both correctly
/// and is what users expect, so it is left alone.
/// </para>
/// </remarks>
public sealed class StorageProviderFileExportDialog : FileExportDialog
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

    private static readonly FilePickerFileType PngFileType = new("Imagem (.png)")
    {
        Patterns = ["*.png"],
        MimeTypes = ["image/png"],
        AppleUniformTypeIdentifiers = ["public.png"],
    };

    /// <inheritdoc/>
    public async Task<ExportTarget?> CreateAsync(string baseName, ExportFileKind kind, Func<string, Task<bool>> confirmReplace)
    {
        ArgumentNullException.ThrowIfNull(confirmReplace);

        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
        if (storageProvider == null)
        {
            return null;
        }

        var fileName = baseName + Extension(kind);

        return OperatingSystem.IsAndroid()
            ? await CreateInFolderAsync(storageProvider, fileName, confirmReplace).ConfigureAwait(true)
            : await CreateThroughSaveDialogAsync(storageProvider, fileName, kind).ConfigureAwait(true);
    }

    /// <inheritdoc/>
    public async Task<Stream?> OpenBackupAsync()
    {
        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
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

    private static string Extension(ExportFileKind kind) => kind == ExportFileKind.Zip ? ".zip" : ".png";

    private static FilePickerFileType FileType(ExportFileKind kind) => kind == ExportFileKind.Zip ? ZipFileType : PngFileType;

    /// <summary>
    /// The Android route: the user chooses a folder and this decides the name, so the extension
    /// survives and an existing file is replaced rather than side-stepped.
    /// </summary>
    private static async Task<ExportTarget?> CreateInFolderAsync(
        IStorageProvider storageProvider,
        string fileName,
        Func<string, Task<bool>> confirmReplace)
    {
        if (!storageProvider.CanPickFolder)
        {
            return null;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Escolher pasta",
            AllowMultiple = false,
        }).ConfigureAwait(true);

        if (folders.Count == 0)
        {
            return null;
        }

        var folder = folders[0];

        // Null means nothing of that name is there, which is the common case and asks nothing.
        var existing = await folder.GetFileAsync(fileName).ConfigureAwait(true);
        if (existing != null && !await confirmReplace(fileName).ConfigureAwait(true))
        {
            return null;
        }

        // Returns the existing file truncated when there is one, so this both overwrites and keeps
        // the name exact. Opening it is what empties it.
        var file = await folder.CreateFileAsync(fileName).ConfigureAwait(true);
        if (file == null)
        {
            return null;
        }

        var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        return new ExportTarget(stream, file.Name);
    }

    /// <summary>
    /// The desktop route: the platform's save dialog names the file, warns about overwriting and
    /// lets the user rename, so nothing here needs to.
    /// </summary>
    private static async Task<ExportTarget?> CreateThroughSaveDialogAsync(
        IStorageProvider storageProvider,
        string fileName,
        ExportFileKind kind)
    {
        if (!storageProvider.CanSave)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = kind == ExportFileKind.Zip ? "Exportar backup" : "Salvar resumo",
            SuggestedFileName = fileName,
            DefaultExtension = Extension(kind).TrimStart('.'),
            FileTypeChoices = [FileType(kind)],
            ShowOverwritePrompt = true,
        }).ConfigureAwait(true);

        if (file == null)
        {
            return null;
        }

        var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        return new ExportTarget(stream, file.Name);
    }
}