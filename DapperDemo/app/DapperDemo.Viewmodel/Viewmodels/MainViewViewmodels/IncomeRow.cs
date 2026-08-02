using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Mensagens.Dapper.Aggregates;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

public class IncomeRow(string label, string amount)
{
    public string Label { get; } = label;

    public string Amount { get; } = amount;
}