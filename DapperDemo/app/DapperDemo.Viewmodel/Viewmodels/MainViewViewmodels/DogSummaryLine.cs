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