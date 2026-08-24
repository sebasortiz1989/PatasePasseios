namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

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