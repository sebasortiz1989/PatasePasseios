using Avalonia;
using Avalonia.Controls;
using Picker = DapperDemo.Viewmodel.Viewmodels.Utils.PeriodPicker;

namespace DapperDemo.View.Components;

/// <summary>
/// The inline month/year picker the four period screens share. Place it directly under the screen's
/// period bar and bind <see cref="Picker"/> to that screen's picker.
/// </summary>
/// <remarks>
/// It hides itself when the picker is closed, so the host only has to decide whether a period
/// control belongs on screen at all.
/// </remarks>
public partial class PeriodPicker : UserControl
{
    public static readonly StyledProperty<Picker?> PickerProperty =
        AvaloniaProperty.Register<PeriodPicker, Picker?>(nameof(Picker));

    public PeriodPicker()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the screen's picker: its open state, its year and its cells.</summary>
    public Picker? Picker
    {
        get => GetValue(PickerProperty);
        set => SetValue(PickerProperty, value);
    }
}