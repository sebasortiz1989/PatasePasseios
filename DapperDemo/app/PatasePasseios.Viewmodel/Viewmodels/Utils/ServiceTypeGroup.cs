using AvaloniaFramework.Presentation;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One kind of service on the dog or tutor screen, with its bookings folded away until tapped.
/// </summary>
/// <remarks>
/// The same shape the agenda uses for <see cref="DogServiceGroup"/>, keyed on the kind instead of
/// the dog: these two screens are already about one dog or one tutor, so the kind is what usefully
/// divides the list — and moving it into the heading is what leaves room for each booking's date.
/// </remarks>
[AddINotifyPropertyChangedInterface]
public sealed class ServiceTypeGroup : IDisposable
{
    public ServiceTypeGroup(string typeLabel, string countLabel, IEnumerable<ServiceGroupRow> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TypeLabel = typeLabel;
        CountLabel = countLabel;
        Services = [.. services];

        // Owned here rather than by the view model: the group is what gets thrown away when the
        // list rebuilds, so the command has to go with it.
        ToggleCommand = new SynchronizedCommand(
            () => IsExpanded = !IsExpanded,
            SynchronizationBehavior.Discard,
            true);
    }

    /// <summary>Gets the kind's name — Passeio, Hotel, and so on.</summary>
    public string TypeLabel { get; }

    /// <summary>Gets how many bookings this kind has under the current filters, e.g. "6 serviços".</summary>
    public string CountLabel { get; }

    /// <summary>Gets or sets a value indicating whether the bookings are showing.</summary>
    public bool IsExpanded { get; set; }

    public ObservableCollection<ServiceGroupRow> Services { get; }

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