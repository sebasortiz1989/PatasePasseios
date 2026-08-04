using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

public sealed class TutorRow(string initials, string name, string subtitle, ICommand openCommand) : IDisposable
{
    public string Initials { get; } = initials;

    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    public ICommand OpenCommand { get; } = openCommand;

    public void Dispose() => (OpenCommand as IDisposable)?.Dispose();
}