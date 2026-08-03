namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

/// <summary>
/// One service type within a dog's summary card, e.g. "Passeio × 6" totalling R$ 300,00.
/// </summary>
public sealed class DogSummaryLine(string typeLabel, int count, string amountLabel)
{
    public string TypeLabel { get; } = typeLabel;

    public int Count { get; } = count;

    /// <summary>Gets the type and how many of them, ready to display: "Passeio × 6".</summary>
    public string CountLabel { get; } = $"{typeLabel} × {count}";

    public string AmountLabel { get; } = amountLabel;
}

/// <summary>
/// A dog's services over the selected timeframe, collapsed to one card. Shown in place of the
/// chronological list when "incluir serviços já pagos" is on, where a flat list of past bookings
/// is long and hard to read.
/// </summary>
public sealed class DogSummaryRow(string dogName, string totalLabel, IReadOnlyList<DogSummaryLine> lines)
{
    public string DogName { get; } = dogName;

    public string TotalLabel { get; } = totalLabel;

    public IReadOnlyList<DogSummaryLine> Lines { get; } = lines;
}
