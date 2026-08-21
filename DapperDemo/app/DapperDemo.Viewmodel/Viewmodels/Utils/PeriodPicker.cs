using AvaloniaFramework.Presentation;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// The inline period picker: a year stepper over a grid of the whole year and the twelve months.
/// </summary>
/// <remarks>
/// <para>
/// Expanded in ordinary layout rather than in a popup, because a popup lays out in its own visual
/// root and so ignores the design canvas' scale — the same reason the app has none anywhere else.
/// </para>
/// <para>
/// It exists because stepping alone made a past month expensive: reaching December of last year
/// meant walking back a month at a time, seven taps or more. Year row, then a month cell, is three.
/// </para>
/// <para>
/// One of these per screen, driven through <see cref="PeriodScope"/>, so the agenda, a dog, a tutor
/// and Perfil cannot drift apart — which is exactly what happened to the stepping logic twice
/// before it was shared.
/// </para>
/// </remarks>
[AddINotifyPropertyChangedInterface]
public sealed class PeriodPicker
{
    private readonly PeriodScope scope;

    /// <summary>Initializes a new instance of the <see cref="PeriodPicker"/> class.</summary>
    /// <param name="scope">The screen whose period this picks.</param>
    public PeriodPicker(PeriodScope scope)
    {
        this.scope = scope;

        ToggleCommand = new SynchronizedCommand(Toggle, SynchronizationBehavior.Discard, true);
        PreviousYearCommand = new SynchronizedCommand(() => StepYear(-1), SynchronizationBehavior.Discard, true);
        NextYearCommand = new SynchronizedCommand(() => StepYear(1), SynchronizationBehavior.Discard, true);
    }

    /// <summary>Gets a value indicating whether the picker is expanded.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Gets the year the picker is showing, as text.</summary>
    public string YearLabel => scope.SelectedYear.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets the whole-year cell plus the twelve months.</summary>
    public ObservableCollection<PeriodCell> Cells { get; } = [];

    /// <summary>Gets the command opening and closing the picker.</summary>
    public ICommand ToggleCommand { get; }

    /// <summary>Gets the command moving back one year without closing the picker.</summary>
    public ICommand PreviousYearCommand { get; }

    /// <summary>Gets the command moving forward one year without closing the picker.</summary>
    public ICommand NextYearCommand { get; }

    /// <summary>
    /// Rebuilds the cells, which is also what re-marks the selected one and relabels the
    /// whole-year cell after a year change.
    /// </summary>
    /// <remarks>
    /// Called from the screen's SelectedMonth and SelectedYear hooks, so the highlight cannot go
    /// stale whatever moved the period — the arrows, this picker, or a filter.
    /// Thirteen short-lived objects, rebuilt rather than mutated, matching every other row list.
    /// </remarks>
    public void Refresh()
    {
        foreach (var cell in Cells)
        {
            cell.Dispose();
        }

        Cells.Clear();

        var current = scope.SelectedMonth?.Number ?? ServicePeriod.WholeYear;

        foreach (var option in scope.MonthOptions)
        {
            var number = option.Number;

            // CA2000: ownership passes to the PeriodCell, which disposes the command when this
            // list is rebuilt above.
#pragma warning disable CA2000
            var select = new SynchronizedCommand(() => Select(number), SynchronizationBehavior.Discard, true);
#pragma warning restore CA2000

            // Three letters so twelve months fit four to a row. The whole-year cell reads as the
            // year itself rather than "Ano todo", which came out clipped in a cell this width —
            // and "2026" beside "jan" says the same thing in the space available.
            var label = number == ServicePeriod.WholeYear
                ? scope.SelectedYear.ToString(CultureInfo.InvariantCulture)
                : ServicePeriod.ShortMonthName(number);

            Cells.Add(new PeriodCell(label, number, number == current, select));
        }
    }

    private void Toggle()
    {
        IsOpen = !IsOpen;

        if (IsOpen)
        {
            Refresh();
        }
    }

    /// <summary>Moves a whole year, leaving the picker open so a month can follow.</summary>
    private void StepYear(int delta)
    {
        var year = scope.SelectedYear + delta;

        if (!scope.YearOptions.Contains(year))
        {
            scope.YearOptions.Add(year);
        }

        scope.SelectedYear = year;
    }

    private void Select(int number)
    {
        scope.SelectedMonth = scope.MonthOptions.FirstOrDefault(m => m.Number == number) ?? scope.SelectedMonth;
        IsOpen = false;
    }
}