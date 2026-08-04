using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One upcoming service on the dog screen. Owns its command so the list can dispose it when the
/// screen reloads, the same way <see cref="ServiceRow"/> does on the agenda.
/// </summary>
public sealed class FutureServiceRow(string typeLabel, string dateLabel, bool paid, string paidLabel, bool done, string doneLabel, ICommand openCommand) : IDisposable
{
    public string TypeLabel { get; } = typeLabel;

    public string DateLabel { get; } = dateLabel;

    /// <summary>Gets a value indicating whether the booking has been settled. Drives the row's tag colour.</summary>
    public bool Paid { get; } = paid;

    public string PaidLabel { get; } = paidLabel;

    /// <summary>Gets a value indicating whether the work has been carried out. Drives its own tag, separate from <see cref="Paid"/>.</summary>
    public bool Done { get; } = done;

    public string DoneLabel { get; } = doneLabel;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}