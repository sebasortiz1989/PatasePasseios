using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One row of the agenda. Owns its open command, so the list can dispose it when the filters
/// rebuild rather than leaking a command per row on every filter change. Paid and done are
/// display-only here — payment is recorded from the tutor screen, and execution is toggled on the
/// service detail screen.
/// </summary>
public sealed class ServiceRow(
    string dayNum,
    string monthShort,
    string dogName,
    string typeLabel,
    string timeLabel,
    string priceLabel,
    bool paid,
    string paidLabel,
    bool done,
    string doneLabel,
    ICommand openCommand) : IDisposable
{
    public string DayNum { get; } = dayNum;

    public string MonthShort { get; } = monthShort;

    public string DogName { get; } = dogName;

    public string TypeLabel { get; } = typeLabel;

    public string TimeLabel { get; } = timeLabel;

    public string PriceLabel { get; } = priceLabel;

    public bool Paid { get; } = paid;

    public string PaidLabel { get; } = paidLabel;

    /// <summary>Gets a value indicating whether the work has been carried out. Drives its own tag, separate from <see cref="Paid"/>.</summary>
    public bool Done { get; } = done;

    public string DoneLabel { get; } = doneLabel;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}