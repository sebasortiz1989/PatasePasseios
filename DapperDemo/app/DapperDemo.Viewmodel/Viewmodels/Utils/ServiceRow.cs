using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One row of the agenda. Owns its three commands, so the list can dispose them when the
/// filters rebuild it rather than leaking a set per row on every filter change.
/// </summary>
public sealed class ServiceRow(string dayNum, string monthShort, string dogName, string typeLabel, string timeLabel, string priceLabel, bool paid, string paidLabel, bool done, string doneLabel, bool canTogglePaid, ICommand openCommand, ICommand toggleCommand, ICommand toggleDoneCommand) : IDisposable
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

    /// <summary>
    /// Gets a value indicating whether the paid tag does anything. Work is only billable once it
    /// has happened, so a booking still to do cannot be settled from here; an already-settled one
    /// stays togglable so a mistake can be undone.
    /// </summary>
    public bool CanTogglePaid { get; } = canTogglePaid;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose()
    {
        (OpenCommand as IDisposable)?.Dispose();
    }
}