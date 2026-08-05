using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One payment on the tutor screen, with the two ways of correcting it.
/// </summary>
/// <remarks>
/// Owns its commands so the list can dispose them when the screen reloads, the same way
/// <see cref="TutorFutureServiceRow"/> does — a screen that reloads on every filter change would
/// otherwise leak a pair of commands per payment per refresh.
/// </remarks>
public sealed class TutorPaymentRow(
    int paymentId,
    string dateLabel,
    string amountLabel,
    string creditLabel,
    ICommand editCommand,
    ICommand deleteCommand) : IDisposable
{
    /// <summary>Gets the ledger entry this row stands for, which the edit and delete act on.</summary>
    public int PaymentId { get; } = paymentId;

    public string DateLabel { get; } = dateLabel;

    public string AmountLabel { get; } = amountLabel;

    /// <summary>
    /// Gets what the payment could not spend and left as credit, or empty when all of it landed on
    /// services. Worth showing on the row: it is the part that is easiest to be surprised by.
    /// </summary>
    public string CreditLabel { get; } = creditLabel;

    public bool HasCredit => !string.IsNullOrEmpty(CreditLabel);

    public ICommand EditCommand { get; } = editCommand;

    public ICommand DeleteCommand { get; } = deleteCommand;

    public void Dispose()
    {
        (EditCommand as IDisposable)?.Dispose();
        (DeleteCommand as IDisposable)?.Dispose();
    }
}