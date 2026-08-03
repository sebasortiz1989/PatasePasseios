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
}
