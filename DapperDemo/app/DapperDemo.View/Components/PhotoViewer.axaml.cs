using Avalonia;
using Avalonia.Controls;
using DapperDemo.View.Imaging;
using System.Windows.Input;

namespace DapperDemo.View.Components;

/// <summary>
/// One photo at the resolution it was stored at, over a scrim covering the screen that hosts it.
/// Place it as the last child of a screen's root Grid, the same way <see cref="ConfirmDialog"/> is.
/// </summary>
/// <remarks>
/// The lists and detail screens decode photos down to the size they draw them at, which is what
/// keeps them scrolling. This is the one place that pays for the full decode, and it only pays
/// while it is open — see <see cref="ActivePath"/>.
/// </remarks>
public partial class PhotoViewer : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<PhotoViewer, bool>(nameof(IsOpen));

    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<PhotoViewer, string?>(nameof(Path));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<PhotoViewer, ICommand?>(nameof(CloseCommand));

    private static readonly StyledProperty<string?> ActivePathProperty =
        AvaloniaProperty.Register<PhotoViewer, string?>(nameof(ActivePath));

    public PhotoViewer()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets a value indicating whether the photo covers its host screen.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the absolute path of the photo to show.</summary>
    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>Gets or sets what dismissing the photo runs. Expected to close the viewer.</summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// Gets the path the image actually loads from: <see cref="Path"/> while open, null otherwise.
    /// </summary>
    /// <remarks>
    /// Binding the image straight to <see cref="Path"/> would decode a full-resolution photo as
    /// soon as the host screen loaded, whether or not the viewer was ever opened — which is the
    /// cost this screen exists to confine. <see cref="IsVisible"/> does not help: a hidden control
    /// still evaluates its bindings.
    /// </remarks>
    public string? ActivePath => GetValue(ActivePathProperty);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty || change.Property == PathProperty)
        {
            SetValue(ActivePathProperty, IsOpen ? Path : null);
        }
    }
}