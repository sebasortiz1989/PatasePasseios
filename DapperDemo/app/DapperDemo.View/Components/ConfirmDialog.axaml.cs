using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace DapperDemo.View.Components;

/// <summary>
/// A modal yes/no confirmation, drawn as a scrim over the screen that hosts it rather than as a
/// window — the app runs single-view on Android and iOS, where a separate window is not available.
/// Place it as the last child of a screen's root Grid so it covers everything below it.
/// </summary>
public partial class ConfirmDialog : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ConfirmDialog, bool>(nameof(IsOpen));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand?>(nameof(ConfirmCommand));

    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand?>(nameof(CancelCommand));

    public static readonly StyledProperty<string> ConfirmTextProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(ConfirmText), "Sim");

    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(CancelText), "Não");

    public static readonly StyledProperty<bool> ShowCancelProperty =
        AvaloniaProperty.Register<ConfirmDialog, bool>(nameof(ShowCancel), true);

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets a value indicating whether the dialog covers its host screen.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the question, e.g. "Excluir cachorro?".</summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets what the user is agreeing to, spelled out.</summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets what "Sim" runs.</summary>
    public ICommand? ConfirmCommand
    {
        get => GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    /// <summary>Gets or sets what "Não" runs. Expected to close the dialog.</summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    /// <summary>Gets or sets the label on the accepting button.</summary>
    public string ConfirmText
    {
        get => GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    /// <summary>Gets or sets the label on the dismissing button.</summary>
    public string CancelText
    {
        get => GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether both buttons are shown. Set it false to make the
    /// dialog an alert — one full-width button that only dismisses, for telling the user something
    /// rather than asking them.
    /// </summary>
    public bool ShowCancel
    {
        get => GetValue(ShowCancelProperty);
        set => SetValue(ShowCancelProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            ScreenOverlay.Current.Set(this, IsOpen);
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ScreenOverlay.Current.Set(this, IsOpen);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // A screen swapped away while its dialog was up takes the dialog with it, so it is no
        // longer covering anything — without this the bar would stay hidden on the next screen.
        ScreenOverlay.Current.Set(this, false);
    }
}