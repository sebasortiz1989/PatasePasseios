namespace PatasePasseios.Repository.Dapper.Dtos;

/// <summary>Billing totals for one month, split the way the Perfil screen shows them.</summary>
public sealed class MonthlyIncome
{
    public decimal Walk { get; init; }

    public decimal Sitting { get; init; }

    public decimal Hotel { get; init; }

    public decimal DayCare { get; init; }

    public decimal Total => Walk + Sitting + Hotel + DayCare;
}