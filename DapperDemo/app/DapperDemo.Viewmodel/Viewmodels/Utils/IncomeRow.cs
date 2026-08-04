namespace DapperDemo.Viewmodel.Viewmodels.Utils;

public class IncomeRow(string label, string amount)
{
    public string Label { get; } = label;

    public string Amount { get; } = amount;
}