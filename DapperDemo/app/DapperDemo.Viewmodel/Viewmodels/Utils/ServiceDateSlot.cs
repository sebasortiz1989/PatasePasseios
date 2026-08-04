using PropertyChanged;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// One day a service happens. Bookings are often a handful of scattered days — the 1st, 3rd and
/// 10th — rather than a continuous run, so the form holds a list of these.
/// </summary>
/// <remarks>
/// Carries its own remove command for the same reason <see cref="ServiceTimeSlot"/> does: reaching
/// back up to the view model from inside an item template needs an explicit cast under compiled
/// bindings.
/// </remarks>
[AddINotifyPropertyChangedInterface]
public class ServiceDateSlot(DateTime date, ICommand removeCommand)
{
    public DateTime Date { get; set; } = date;

    /// <summary>Gets removes this row from the list. Hidden while it is the only one left.</summary>
    public ICommand RemoveCommand { get; } = removeCommand;

    /// <summary>Gets or sets a value indicating whether whether this row may be removed; the form always needs at least one date.</summary>
    public bool CanRemove { get; set; } = true;
}