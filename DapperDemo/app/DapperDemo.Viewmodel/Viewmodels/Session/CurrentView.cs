using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

public class CurrentView : INotifyPropertyChanged
{
    /// <summary>
    /// Screens left behind by <see cref="ViewShown"/>, most recent first. A stack rather than a
    /// single previous view because detail screens now open other detail screens: a service opened
    /// from a tutor has to return to that tutor, and the tutor's own Back still has to return to
    /// the tab underneath it.
    /// </summary>
    private readonly Stack<object> history = new();

    private object? viewShown;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Nullable: nothing is shown before the first tab is selected.
    public object? ViewShown
    {
        get => viewShown;
        set
        {
            if (EqualityComparer<object?>.Default.Equals(viewShown, value))
            {
                return;
            }

            if (viewShown is { } previous)
            {
                history.Push(previous);
            }

            SetField(ref viewShown, value);
        }
    }

    /// <summary>
    /// Shows a top-level tab, discarding any detail screens stacked above it. Without this a tab
    /// switch would leave the abandoned detail screens on the stack, and Back from the next detail
    /// screen would walk into a tab the user had already navigated away from.
    /// </summary>
    public void ShowRoot(object? view)
    {
        history.Clear();

        if (EqualityComparer<object?>.Default.Equals(viewShown, view))
        {
            return;
        }

        SetField(ref viewShown, view, nameof(ViewShown));
    }

    public void GoBack()
    {
        if (history.TryPop(out var previous))
        {
            SetField(ref viewShown, previous, nameof(ViewShown));
        }
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
}