using DapperDemo.Repository.Dapper.Dtos;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

/// <summary>
/// Collapses a set of services into one card per dog. Shared by the home screen's paid summary and
/// the profile screen's paid/pending breakdown so the two cannot drift apart in how they group or
/// what they consider a booking to be worth.
/// </summary>
internal static class DogSummaryBuilder
{
    /// <summary>
    /// What a booking is worth. A hotel stay is entered as a daily rate, so its value is that rate
    /// multiplied by the nights booked; everything else is its price as stored.
    /// </summary>
    public static decimal AmountOf(ServiceItem service)
    {
        if (service.Kind != ServiceKind.Hotel)
        {
            return service.Price;
        }

        var nights = service.EndDate is DateTime end
            ? Math.Max((end.Date - service.Date.Date).Days, 1)
            : 1;

        return service.Price * nights;
    }

    /// <summary>
    /// One row per dog, alphabetical, each listing its service types with counts and amounts.
    /// </summary>
    /// <param name="services">The services already narrowed to the period and filters in play.</param>
    /// <param name="money">
    /// How to render an amount. Passed in because the profile screen bullets figures out while its
    /// eye is closed, whereas the home screen always shows them.
    /// </param>
    public static List<DogSummaryRow> Build(IEnumerable<ServiceItem> services, Func<decimal, string> money)
    {
        return services
            .GroupBy(s => s.DogName, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .Select(dog => new DogSummaryRow(
                dog.Key,
                money(dog.Sum(AmountOf)),
                dog.GroupBy(s => s.Kind)
                    .OrderBy(g => g.Key)
                    .Select(g => new DogSummaryLine(
                        AppSession.TypeLabel(g.Key),
                        g.Count(),
                        money(g.Sum(AmountOf))))
                    .ToArray()))
            .ToList();
    }
}
