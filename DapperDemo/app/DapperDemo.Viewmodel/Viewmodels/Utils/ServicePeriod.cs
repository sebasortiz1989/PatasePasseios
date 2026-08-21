using DapperDemo.Repository.Dapper.Dtos;
using System.Globalization;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// The month/year picker shared by the dog and tutor screens: the option lists both fill their
/// combo boxes from, and the rule that decides whether a booking falls in the chosen period.
/// </summary>
/// <remarks>
/// A helper rather than a base class because each screen owns its own Fody-notified properties —
/// the weaver's <c>On&lt;Property&gt;Changed</c> hooks only fire on the view model itself. This
/// keeps the logic in one place while the bindable state stays where the view can reach it.
/// </remarks>
internal static class ServicePeriod
{
    /// <summary>The month number standing for "no month filter, just the year".</summary>
    public const int WholeYear = 0;

    private static readonly CultureInfo Brazil = new("pt-BR");

    private static readonly string[] Abbreviations = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    /// <summary>
    /// The month's three-letter abbreviation, lower case, e.g. "ago".
    /// </summary>
    /// <remarks>
    /// For the places a full month name will not fit: the agenda's date column and the picker's
    /// grid, which puts four months to a row.
    /// </remarks>
    /// <param name="month">Month number, 1 to 12.</param>
    /// <returns>The abbreviation.</returns>
    public static string ShortMonthName(int month) => Abbreviations[month - 1];

    /// <summary>The whole-year entry followed by the twelve months. Fixed, so it never rebuilds.</summary>
    public static IEnumerable<MonthOption> Months()
    {
        yield return new MonthOption(WholeYear, "Ano todo");

        for (var month = 1; month <= 12; month++)
        {
            var name = Brazil.DateTimeFormat.GetMonthName(month);
            yield return new MonthOption(month, char.ToUpper(name[0], Brazil) + name[1..]);
        }
    }

    /// <summary>
    /// The period as one line, e.g. "Agosto 2026" or "Ano todo de 2026".
    /// </summary>
    /// <param name="month">The chosen month, or null.</param>
    /// <param name="year">The chosen year.</param>
    /// <returns>What the stepper shows between its arrows.</returns>
    public static string Label(MonthOption? month, int year) =>
        month is not { } chosen || chosen.Number == WholeYear
            ? $"Ano todo de {year.ToString(CultureInfo.InvariantCulture)}"
            : $"{chosen.Label} {year.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Moves a period by whole months, carrying into the next or previous year at the ends.
    /// </summary>
    /// <remarks>
    /// Shared by the four screens that scope a list to a period, so stepping cannot mean one thing
    /// on the agenda and another on a ficha.
    /// <para>
    /// The arrows do one of two things depending on where they are. On a month they move by a
    /// month and carry into the next or previous year at the ends. On <c>Ano todo</c> they move by
    /// a <b>year</b> — because there is no month to step, and a whole-year view of 2025 is the
    /// thing next to a whole-year view of 2026. Together with <see cref="ToggleWholeYear"/> that
    /// reaches every month of every year and every year as a whole, which is what the two
    /// drop-downs this replaced could do.
    /// </para>
    /// </remarks>
    /// <param name="month">The month now shown, or null.</param>
    /// <param name="year">The year now shown.</param>
    /// <param name="delta">−1 or +1.</param>
    /// <returns>The month number and year to move to.</returns>
    public static (int Month, int Year) Step(MonthOption? month, int year, int delta)
    {
        var current = month?.Number ?? DateTime.Now.Month;

        // Nothing to step within a whole year, so the arrows change which year it is.
        if (current == WholeYear)
        {
            return (WholeYear, year + delta);
        }

        var next = current + delta;

        if (next < 1)
        {
            return (12, year - 1);
        }

        return next > 12 ? (1, year + 1) : (next, year);
    }

    /// <summary>
    /// Switches between a single month and the whole year, staying in the same year.
    /// </summary>
    /// <remarks>
    /// The way back to <c>Ano todo</c> once the arrows have moved off it. Leaving it lands on the
    /// current calendar month rather than January, because "this month" is what the screen opens
    /// on and what the sitter is usually after.
    /// </remarks>
    /// <param name="month">The month now shown, or null.</param>
    /// <returns>The month number to move to.</returns>
    public static int ToggleWholeYear(MonthOption? month) =>
        month is { Number: not WholeYear } ? WholeYear : DateTime.Now.Month;

    /// <summary>
    /// The years worth offering: every year these services touch, plus the current one so a dog
    /// with nothing booked still has something selectable. Most recent first.
    /// </summary>
    /// <param name="alsoFrom">
    /// Extra dates that must stay reachable, such as the tutor's payments — a payment made in a
    /// year with no bookings would otherwise have no year to select it by.
    /// </param>
    /// <param name="alsoYear">
    /// The year currently on screen. Included even when nothing falls in it, because the rebuild
    /// that follows drops any selected year missing from this list — so stepping to a quiet year
    /// snapped straight back to one with bookings, and the arrows looked broken.
    /// </param>
    public static int[] Years(IEnumerable<ServiceItem> services, IEnumerable<DateTime>? alsoFrom = null, int? alsoYear = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return [.. services
            .Select(s => s.Date.Year)
            .Concat(alsoFrom?.Select(d => d.Year) ?? [])
            .Concat(alsoYear is { } year ? new[] { year } : [])
            .Append(DateTime.Now.Year)
            .Distinct()
            .OrderByDescending(year => year)];
    }

    /// <summary>Whether a booking falls inside the selected period.</summary>
    /// <param name="service">The booking to test.</param>
    /// <param name="month">The selected month, or the whole-year entry.</param>
    /// <param name="year">The selected year.</param>
    public static bool Matches(ServiceItem service, MonthOption? month, int year)
    {
        ArgumentNullException.ThrowIfNull(service);

        return Matches(service.Date, month, year);
    }

    /// <summary>
    /// Whether a date falls inside the selected period. The tutor screen scopes its payment list
    /// with the same pickers it scopes its service list with, and a payment is only a date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="month">The selected month, or the whole-year entry.</param>
    /// <param name="year">The selected year.</param>
    /// <returns>Whether it falls in the period.</returns>
    public static bool Matches(DateTime date, MonthOption? month, int year)
    {
        if (date.Year != year)
        {
            return false;
        }

        return month is not { Number: not WholeYear } chosen || date.Month == chosen.Number;
    }
}