using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One service inside a <see cref="ServiceTypeGroup"/> on the dog or tutor screen.
/// </summary>
/// <remarks>
/// The kind is not on the row: it is the group's heading, which is what frees the row's widest
/// column for the date. On the tutor screen the dog's name leads the row, since a tutor has
/// several; on the dog screen it is suppressed, because repeating the one dog's name would refill
/// the space the grouping just freed.
/// </remarks>
public sealed class ServiceGroupRow(
    string dogName,
    bool showsDogName,
    string dateLabel,
    bool paid,
    string paidLabel,
    bool done,
    string doneLabel,
    ICommand openCommand) : IDisposable
{
    public string DogName { get; } = dogName;

    /// <summary>Gets a value indicating whether the dog's name is worth showing on this screen.</summary>
    public bool ShowsDogName { get; } = showsDogName;

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