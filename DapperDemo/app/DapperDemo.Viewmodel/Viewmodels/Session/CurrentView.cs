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
    /// What the screen currently shown would be called by the screen above it. Held so that
    /// pushing the next screen can put this one on the stack under its own name.
    /// </summary>
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
    /// Pushes a screen on top of the current one.
    /// </summary>
    /// <param name="view">The screen's presenter.</param>
    /// <param name="label">
    /// What this screen should be called by whatever opens next on top of it — a record's own name
    /// where the caller has one, so Back reads "Rex" rather than "Cachorro".
    /// </param>
    public void Show(object? view, string label)
    {
        if (EqualityComparer<object?>.Default.Equals(viewShown, view))
        {
            return;
        }

        if (viewShown is { } previous)
        {
            history.Push(new Entry(previous, currentLabel));
        }

        currentLabel = label;
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