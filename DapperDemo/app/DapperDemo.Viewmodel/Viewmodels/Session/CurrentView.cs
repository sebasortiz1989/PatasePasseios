using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

/// <summary>
/// Which screen is on show, and the trail of screens behind it.
/// </summary>
/// <remarks>
/// Detail screens stack: opening a tutor from a dog, a dog from that tutor and a tutor again
/// leaves three entries, and Back walks them one at a time until it reaches the tab the branch
/// started from. That is what a person expects of Back, and it is what this used to refuse to do.
/// <para>
/// It refused for a real reason. The presenters are reused instances and the record each one shows
/// lives on <see cref="AppSession"/>, so an entry that remembered only "the dog screen" did not
/// remember <em>which dog</em> — walking back into it re-rendered it with whatever was selected
/// now, which made the history a loop rather than a path. Every entry therefore carries the
/// <see cref="Selection"/> that was current when its screen was shown, and <see cref="GoBack"/>
/// puts it back before the screen reappears. That is the whole trick; without it, stacking is
/// worse than flattening.
/// </para>
/// </remarks>
/// <param name="session">Where the selected records live, so an entry can capture and restore them.</param>
public class CurrentView(AppSession session) : INotifyPropertyChanged
{
    /// <summary>Screens left behind by <see cref="Show"/>, most recent first.</summary>
    private readonly Stack<Entry> history = new();

    private object? viewShown;

    /// <summary>What the screen on show is called, which is what the screen above it goes back to.</summary>
    private string currentLabel = string.Empty;

    /// <summary>
    /// The record the screen on show was opened with.
    /// </summary>
    /// <remarks>
    /// Captured when the screen is shown rather than read off the session when it is pushed. By
    /// push time the caller has already selected the record for the screen it is opening, so
    /// reading the session then would store the wrong one.
    /// </remarks>
    private Selection currentSelection;

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
    /// markup. A tutor opened from a dog goes back to that dog and the label has to say so — a
    /// hardcoded "Tutores" was wrong in every case except the one route the author had in mind.
    /// </remarks>
    public string BackLabel => history.TryPeek(out var previous) ? previous.Label : string.Empty;

    /// <summary>
    /// Shows a screen on top of the one already there. Back returns to it.
    /// </summary>
    /// <param name="view">The screen's presenter.</param>
    /// <param name="label">
    /// What this screen is called — a dog's or tutor's name, a booking's kind, a form's title. It
    /// is what the back control of anything opened from here will read, so it names the record and
    /// not the screen type: "Jony", never "Cachorro".
    /// </param>
    public void Show(object? view, string label)
    {
        // A second tap on the same row, which would otherwise put the screen on its own stack.
        if (EqualityComparer<object?>.Default.Equals(viewShown, view))
        {
            return;
        }

        if (viewShown is { } previous)
        {
            history.Push(new Entry(previous, currentLabel, currentSelection));
        }

        currentLabel = label;

        // Now, not on the way out: the caller selected this screen's record immediately before
        // calling, so this is the moment the session describes the screen being shown.
        currentSelection = session.Selection;

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
        currentSelection = session.Selection;
        ViewShown = view;
        OnPropertyChanged(nameof(BackLabel));
    }

    /// <summary>Returns to the screen underneath, showing the record it was showing.</summary>
    public void GoBack()
    {
        if (!history.TryPop(out var previous))
        {
            return;
        }

        // The record before the screen. A presenter reloads from its view's OnLoaded, which runs
        // once it is back on the visual tree and reads whatever is selected at that moment.
        session.Selection = previous.Selection;

        currentLabel = previous.Label;
        currentSelection = previous.Selection;
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

    /// <summary>One screen on the back stack: what it was, what it was called, what it was showing.</summary>
    private readonly record struct Entry(object View, string Label, Selection Selection);
}
