using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DapperDemo.Viewmodel.Viewmodels.Session;

public class CurrentView : INotifyPropertyChanged
{
    private object? viewShown;
    private object? lastView;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Nullable: nothing is shown before the first tab is selected, and GoBack from the first
    // screen has no previous view to return to.
    public object? ViewShown
    {
        get => viewShown;
        set
        {
            lastView = viewShown;
            SetField(ref viewShown, value);
        }
    }

    public void GoBack()
    {
        ViewShown = lastView;
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