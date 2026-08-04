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
    /// The years worth offering: every year these services touch, plus the current one so a dog
    /// with nothing booked still has something selectable. Most recent first.
    /// </summary>
    public static int[] Years(IEnumerable<ServiceItem> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return [.. services
            .Select(s => s.Date.Year)
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

        if (service.Date.Year != year)
        {
            return false;
        }

        return month is not { Number: not WholeYear } chosen || service.Date.Month == chosen.Number;
    }
}
