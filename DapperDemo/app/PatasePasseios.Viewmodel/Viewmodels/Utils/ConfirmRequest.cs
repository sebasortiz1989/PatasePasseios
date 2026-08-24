using AvaloniaFramework.Presentation;
using PropertyChanged;
using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

/// <summary>
/// A yes/no question the asker can await, backed by a <c>ConfirmDialog</c> on the screen.
/// </summary>
/// <remarks>
/// <para>
/// The app's other confirmations are a boolean and two commands on the view model, which suits a
/// question the user starts — tapping Excluir puts the dialog up, and the commands carry on from
/// there. This one is asked in the middle of work that is already under way, so the caller needs
/// the answer as a value rather than as a later callback.
/// </para>
/// <para>
/// Bind a dialog's <c>IsOpen</c>, <c>Message</c>, <c>ConfirmCommand</c> and <c>CancelCommand</c> to
/// one of these and the awaited task completes when the user taps.
/// </para>
/// </remarks>
[AddINotifyPropertyChangedInterface]
public sealed class ConfirmRequest
{
    private TaskCompletionSource<bool>? pending;

    /// <summary>Initializes a new instance of the <see cref="ConfirmRequest"/> class.</summary>
    public ConfirmRequest()
    {
        ConfirmCommand = new SynchronizedCommand(() => Answer(true), SynchronizationBehavior.Discard, true);
        CancelCommand = new SynchronizedCommand(() => Answer(false), SynchronizationBehavior.Discard, true);
    }

    /// <summary>Gets a value indicating whether the question is on screen.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Gets the question being asked.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Gets the command the Sim button runs.</summary>
    public ICommand ConfirmCommand { get; }

    /// <summary>Gets the command the Não button runs.</summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Puts the question up and completes when the user answers.
    /// </summary>
    /// <returns>True if the user confirmed.</returns>
    public Task<bool> AskAsync(string message)
    {
        // A second question arriving while one is up would strand the first caller awaiting a task
        // nothing will ever complete, so the older one is answered no and released.
        pending?.TrySetResult(false);

        Message = message;
        IsOpen = true;

        // Asynchronous continuations: the answer is delivered from a command, and resuming the
        // awaiting export inline would run it inside the button's own handler.
        pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return pending.Task;
    }

    private void Answer(bool confirmed)
    {
        IsOpen = false;

        var completion = pending;
        pending = null;
        completion?.TrySetResult(confirmed);
    }
}