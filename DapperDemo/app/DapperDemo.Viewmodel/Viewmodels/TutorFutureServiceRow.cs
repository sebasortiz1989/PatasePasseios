using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels;

/// <summary>
/// One upcoming service on the tutor screen. Owns its command so the list can dispose it when the
/// screen reloads, the same way <see cref="MainViewViewmodels.ServiceRow"/> does on the agenda.
/// </summary>
public sealed class TutorFutureServiceRow(string dogName, string typeLabel, string dateLabel, bool paid, string paidLabel, ICommand openCommand) : IDisposable
{
    public string DogName { get; } = dogName;

    public string TypeLabel { get; } = typeLabel;

    public string DateLabel { get; } = dateLabel;

    /// <summary>Gets a value indicating whether the booking has been settled. Drives the row's tag colour.</summary>
    public bool Paid { get; } = paid;

    public string PaidLabel { get; } = paidLabel;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}
