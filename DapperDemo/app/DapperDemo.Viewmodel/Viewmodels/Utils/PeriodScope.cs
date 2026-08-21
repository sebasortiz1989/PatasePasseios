using System.Collections.ObjectModel;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// A screen that scopes its list to a month or a whole year.
/// </summary>
/// <remarks>
/// The four screens that do this — the agenda, a dog, a tutor and Perfil — already had these four
/// members with these exact names. Naming the shape lets <see cref="PeriodPicker"/> drive all four
/// instead of each carrying its own copy of the picker. Not I-prefixed, matching the framework's
/// convention.
/// </remarks>
public interface PeriodScope
{
    /// <summary>Gets "Ano todo" plus the twelve months.</summary>
    ObservableCollection<MonthOption> MonthOptions { get; }

    /// <summary>Gets the years worth offering, most recent first.</summary>
    ObservableCollection<int> YearOptions { get; }

    /// <summary>Gets or sets the month shown, or the whole-year entry.</summary>
    MonthOption? SelectedMonth { get; set; }

    /// <summary>Gets or sets the year shown.</summary>
    int SelectedYear { get; set; }
}