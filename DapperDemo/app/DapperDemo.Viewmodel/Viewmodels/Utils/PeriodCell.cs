using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One tappable cell of the inline period picker: a month, or the whole year.
/// </summary>
/// <remarks>
/// Carries its own command, the way every other row list in the app does, so the month grid needs
/// no <c>$parent</c> binding back to the screen's view model.
/// </remarks>
/// <param name="label">What the cell reads, e.g. "Ago" or "Ano todo".</param>
/// <param name="number">The month number the query needs, or 0 for the whole year.</param>
/// <param name="isSelected">Whether this is the period currently shown.</param>
/// <param name="selectCommand">Selects this period and closes the picker.</param>
public sealed class PeriodCell(string label, int number, bool isSelected, ICommand selectCommand) : IDisposable
{
    public string Label { get; } = label;

    public int Number { get; } = number;

    /// <summary>Gets a value indicating whether this cell is the period on screen.</summary>
    public bool IsSelected { get; } = isSelected;

    public ICommand SelectCommand { get; } = selectCommand;

    public void Dispose() => (SelectCommand as IDisposable)?.Dispose();
}