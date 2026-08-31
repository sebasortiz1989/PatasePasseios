namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

/// <summary>
/// The span of days a period covers, half open: <see cref="Start"/> is inside it and
/// <see cref="End"/> is the first moment after it.
/// </summary>
/// <remarks>
/// <para>
/// Half open rather than a pair of inclusive dates because a booking carries a time of day. An
/// inclusive end would have to be compared against the last tick of that day, and every call site
/// that forgot would quietly drop the last day's afternoon work off the bill.
/// </para>
/// <para>
/// It exists so a screen can be scoped by a month, a whole year or an arbitrary run of days without
/// three sets of filters: <see cref="ServicePeriod"/> turns each of those into one of these, and the
/// screen only ever asks <see cref="Contains"/>.
/// </para>
/// </remarks>
/// <param name="Start">The period's first moment.</param>
/// <param name="End">The first moment after the period.</param>
internal readonly record struct PeriodRange(DateTime Start, DateTime End)
{
    /// <summary>Whether a date falls inside the period.</summary>
    /// <param name="date">The date to test.</param>
    /// <returns>Whether it is on or after <see cref="Start"/> and before <see cref="End"/>.</returns>
    public bool Contains(DateTime date) => date >= Start && date < End;
}