using AvaloniaFramework.Presentation;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper;
using DapperDemo.Viewmodel.Reports;
using DapperDemo.Viewmodel.Services;
using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// A rendered report held on screen, with the two things a person wants to do with it: send it
/// somewhere, or keep it.
/// </summary>
/// <remarks>
/// <para>
/// The export used to open a save dialog and write a file the user then had to go and find. This
/// shows the picture first and offers the actions beside it, which is the order a phone uses for a
/// screenshot — and it makes sharing possible without saving anything at all, because the image
/// already exists in the cache by the time the sheet opens.
/// </para>
/// <para>
/// Shared by the two screens that export a report rather than duplicated across them. Bind a
/// <c>ReportPreview</c> component to one of these.
/// </para>
/// </remarks>
[AddINotifyPropertyChangedInterface]
public sealed class ReportPreview
{
    private readonly ReportExporter exporter;
    private readonly ShareSheet shareSheet;

    /// <summary>The name the saved copy is offered under, without extension.</summary>
    private string baseName = string.Empty;

    /// <summary>Asked before replacing a file that is already there. Supplied by the owning screen.</summary>
    private Func<string, Task<bool>>? confirmReplace;

    /// <summary>Initializes a new instance of the <see cref="ReportPreview"/> class.</summary>
    public ReportPreview(ReportExporter exporter, ShareSheet shareSheet)
    {
        this.exporter = exporter;
        this.shareSheet = shareSheet;

        ShareCommand = new SynchronizedCommand(Share, SynchronizationBehavior.Discard, true);
        SaveCommand = new SynchronizedCommand(Save, SynchronizationBehavior.Discard, true);
        CloseCommand = new SynchronizedCommand(Close, SynchronizationBehavior.Discard, true);
    }

    /// <summary>Gets a value indicating whether the rendered report is on screen.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Gets the rendered PNG's path, which the preview's image loads from.</summary>
    public string? ImagePath { get; private set; }

    /// <summary>
    /// Gets a value indicating whether to offer sharing. False on the desktop heads, which have no
    /// system share sheet — the button is hidden rather than shown and then failing.
    /// </summary>
    public bool CanShare => shareSheet.CanShare;

    /// <summary>Gets what happened to the report, e.g. "Salvo como faturamento-2026-08.png".</summary>
    public string Message { get; private set; } = string.Empty;

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    public ICommand ShareCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand CloseCommand { get; }

    /// <summary>
    /// Renders a report and puts it on screen.
    /// </summary>
    /// <param name="report">The report's content.</param>
    /// <param name="suggestedFileName">The file's name, without extension.</param>
    /// <param name="replaceConfirmation">Asked before overwriting, when saving needs to ask.</param>
    /// <returns>Successful once the image is showing, or Failed if it could not be rendered.</returns>
    public async Task<Response> ShowAsync(
        ReportDocument report,
        string suggestedFileName,
        Func<string, Task<bool>> replaceConfirmation)
    {
        baseName = suggestedFileName;
        confirmReplace = replaceConfirmation;
        Message = string.Empty;

        // The previous preview's file goes now rather than being left for the system to reclaim:
        // these are a megabyte each and a sitter checking a few months would leave a pile of them.
        Discard();

        var path = await exporter.RenderAsync(report, suggestedFileName).WithSync();
        if (path == null)
        {
            return Response.Failed;
        }

        ImagePath = path;
        IsOpen = true;
        return Response.Successful;
    }

    private async Task Share()
    {
        if (ImagePath is not { Length: > 0 } path)
        {
            return;
        }

        var result = await shareSheet.ShareFileAsync(path, "Compartilhar relatório").WithSync();

        // Left open on success: the share sheet covers this screen, and closing underneath it would
        // put the user somewhere else when they come back from WhatsApp.
        if (result != Response.Successful)
        {
            Message = "Não foi possível abrir o compartilhamento.";
        }
    }

    private async Task Save()
    {
        if (ImagePath is not { Length: > 0 } path || confirmReplace is not { } confirm)
        {
            return;
        }

        var name = await exporter.SaveAsync(path, baseName, confirm).WithSync();

        // Null means the user backed out of the save dialog, which is not a failure to report.
        if (name != null)
        {
            Message = $"Salvo como {name}.";
        }
    }

    private Task Close()
    {
        IsOpen = false;
        Message = string.Empty;
        Discard();
        return Task.CompletedTask;
    }

    /// <summary>Deletes the rendered file, if there is one. A leftover preview is just cache.</summary>
    private void Discard()
    {
        if (ImagePath is not { Length: > 0 } path)
        {
            return;
        }

        ImagePath = null;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Windows will not delete a file the previous preview's Image still holds open. It is
            // in the temporary folder either way, so leaving it is harmless.
            Console.WriteLine(e);
        }
    }
}