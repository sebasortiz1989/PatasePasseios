using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

public class CurrentView : INotifyPropertyChanged
{
    /// <summary>
    /// Screens left behind by <see cref="Show"/>, most recent first. A stack rather than a
    /// single previous view because detail screens now open other detail screens: a service opened
    /// from a tutor has to return to that tutor, and the tutor's own Back still has to return to
    /// the tab underneath it.
    /// </summary>
    private readonly Stack<Entry> history = new();

    private object? viewShown;

    /// <summary>
    /// The name of the tab this branch of navigation started from.
    /// </summary>
    /// <remarks>
    /// Only tabs are ever pushed, so this is the label that goes on the stack with the tab and
    /// comes back off it on the way home. Detail screens do not set it — they are never a back
    /// target, so they are never named.
    /// </remarks>
    private string currentLabel = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Nullable: nothing is shown before the first tab is selected.
    public object? ViewShown
    {
        get => viewShown;
        private set => SetField(ref viewShown, value);
    }

    /// <summary>
    /// Gets the name of the screen <see cref="GoBack"/> would return to, for the back control's
    /// label. Empty when there is nothing to go back to.
    /// </summary>
    /// <remarks>
    /// This is the label of the screen actually underneath, not a constant written into the
    /// markup. A tutor opened from a dog goes back to that dog, and the label has to say so — a
    /// hardcoded "Tutores" was wrong in every case except the one route the author had in mind.
    /// </remarks>
    public string BackLabel => history.TryPeek(out var previous) ? previous.Label : string.Empty;

    /// <summary>
    /// Shows a detail screen. Back from it returns to the tab it was opened from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Detail screens replace each other rather than stacking, so the history is never deeper than
    /// one screen above a tab. A dog opens its tutor, whose dog list opens another dog, whose tutor
    /// opens again — left to stack that grows without bound, and Back has to be pressed once per
    /// hop to escape.
    /// </para>
    /// <para>
    /// Depth is not the only reason. The presenters are reused instances and the selected record
    /// lives on <see cref="AppSession"/>, so a stacked entry does not remember which dog it was
    /// showing: walking back through one would re-render it with whatever record is selected now.
    /// A stack of those is not history, it is a loop.
    /// </para>
    /// </remarks>
    /// <param name="view">The screen's presenter.</param>
    public void Show(object? view)
    {
        if (EqualityComparer<object?>.Default.Equals(viewShown, view))
        {
            return;
        }

        // Only a tab is ever pushed. Arriving here with something already on the stack means the
        // current screen is itself a detail, and it is replaced rather than added to.
        if (history.Count == 0 && viewShown is { } previous)
        {
            history.Push(new Entry(previous, currentLabel));
        }

        ViewShown = view;
        OnPropertyChanged(nameof(BackLabel));
    }

    /// <summary>
    /// Shows a top-level tab, discarding any detail screens stacked above it. Without this a tab
    /// switch would leave the abandoned detail screens on the stack, and Back from the next detail
    /// screen would walk into a tab the user had already navigated away from.
    /// </summary>
    /// <param name="view">The tab's presenter.</param>
    /// <param name="label">The tab's name, which is what the first detail screen's Back reads.</param>
    public void ShowRoot(object? view, string label)
    {
        history.Clear();
        currentLabel = label;
        ViewShown = view;
        OnPropertyChanged(nameof(BackLabel));
    }

    public void GoBack()
    {
        if (!history.TryPop(out var previous))
        {
            return;
        }

        currentLabel = previous.Label;
        ViewShown = previous.View;
        OnPropertyChanged(nameof(BackLabel));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    /// <summary>One screen on the back stack, and the name the screen above it calls it by.</summary>
    private readonly record struct Entry(object View, string Label);
}