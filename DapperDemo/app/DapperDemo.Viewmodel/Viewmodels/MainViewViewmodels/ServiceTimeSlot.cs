using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.MainViewViewmodels;

/// <summary>
/// One time of day a service happens. A pet sitting booking is often twice daily — morning and
/// night — so the new-service form holds a list of these rather than a single time.
/// </summary>
/// <remarks>
/// The row carries its own remove command. The alternative is reaching back up to the view model
/// from inside the item template, which compiled bindings only allow with an explicit cast.
/// </remarks>
[AddINotifyPropertyChangedInterface]
public class ServiceTimeSlot(TimeSpan time, ICommand removeCommand)
{
    public TimeSpan Time { get; set; } = time;

    /// <summary>Removes this row from the list. Disabled while it is the only one left.</summary>
    public ICommand RemoveCommand { get; } = removeCommand;

    /// <summary>Whether this row may be removed; the form always needs at least one time.</summary>
    public bool CanRemove { get; set; } = true;
}