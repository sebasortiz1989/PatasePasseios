using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One dog on the agenda's grouped list, with its services folded away until tapped.
/// </summary>
    /// <remarks>
    /// Grouping rather than listing flat because widening the agenda to include settled work turns it
    /// into hundreds of rows across every dog. The rows inside are ordinary
    /// <see cref="ServiceRow"/> instances, so an expanded group behaves exactly like the flat list —
    /// same tags, same tap-through to the service screen.
    /// </remarks>
[AddINotifyPropertyChangedInterface]
public sealed class DogServiceGroup : IDisposable
{
    public DogServiceGroup(string dogName, string countLabel, string totalLabel, IEnumerable<ServiceRow> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        DogName = dogName;
        CountLabel = countLabel;
        TotalLabel = totalLabel;
        Services = [.. services];

        // Owned here rather than by the view model: the group is what gets thrown away when the
        // filters rebuild, so the command has to go with it.
        ToggleCommand = new AvaloniaFramework.Presentation.SynchronizedCommand(
            () => IsExpanded = !IsExpanded,
            AvaloniaFramework.Presentation.SynchronizationBehavior.Discard,
            true);
    }

    public string DogName { get; }

    /// <summary>Gets how many services this dog has under the current filters, e.g. "6 serviços".</summary>
    public string CountLabel { get; }

    /// <summary>
    /// Gets what those services come to, e.g. "R$ 540,00".
    /// </summary>
    /// <remarks>
    /// On the header so a collapsed group still states what it holds — the count and the money are
    /// the two facts that decide whether the group is worth opening.
    /// </remarks>
    public string TotalLabel { get; }

    /// <summary>Gets or sets a value indicating whether the dog's services are showing.</summary>
    public bool IsExpanded { get; set; }

    public ObservableCollection<ServiceRow> Services { get; }

    public ICommand ToggleCommand { get; }

    public void Dispose()
    {
        foreach (var service in Services)
        {
            service.Dispose();
        }

        Services.Clear();
        (ToggleCommand as IDisposable)?.Dispose();
    }
}