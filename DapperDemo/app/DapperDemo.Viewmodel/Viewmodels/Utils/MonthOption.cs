namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One entry of the billing screen's month picker: the number the query needs, and the
/// Portuguese name the user reads.
/// </summary>
/// <param name="number">Month number, 1 to 12.</param>
/// <param name="label">The month's name, already capitalised.</param>
public sealed class MonthOption(int number, string label)
{
    public int Number { get; } = number;

    public string Label { get; } = label;
}