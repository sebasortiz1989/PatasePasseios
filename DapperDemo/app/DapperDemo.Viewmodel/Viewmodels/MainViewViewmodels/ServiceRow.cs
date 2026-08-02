using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Mensagens.Dapper.Dtos;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

/// <summary>
/// One row of the agenda. Owns its two commands, so the list can dispose them when the
/// filters rebuild it rather than leaking a pair per row on every filter change.
/// </summary>
public sealed class ServiceRow(string dayNum, string monthShort, string dogName, string typeLabel, string timeLabel, string priceLabel, bool paid, string paidLabel, ICommand openCommand, ICommand toggleCommand) : IDisposable
{
    public string DayNum { get; } = dayNum;

    public string MonthShort { get; } = monthShort;

    public string DogName { get; } = dogName;

    public string TypeLabel { get; } = typeLabel;

    public string TimeLabel { get; } = timeLabel;

    public string PriceLabel { get; } = priceLabel;

    public bool Paid { get; } = paid;

    public string PaidLabel { get; } = paidLabel;

    public ICommand OpenCommand { get; } = openCommand;

    public ICommand ToggleCommand { get; } = toggleCommand;

    public void Dispose()
    {
        (OpenCommand as IDisposable)?.Dispose();
        (ToggleCommand as IDisposable)?.Dispose();
    }
}