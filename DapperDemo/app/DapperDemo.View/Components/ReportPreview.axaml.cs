using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace DapperDemo.View.Components;

/// <summary>
/// A rendered report over a scrim, with Compartilhar and Salvar beneath it. Place it as the last
/// child of a screen's root Grid, the same way <see cref="ConfirmDialog"/> is.
/// </summary>
/// <remarks>
/// Bind the properties to a <c>Viewmodel.Viewmodels.Utils.ReportPreview</c>, which owns the
/// rendered file and both actions.
/// </remarks>
public partial class ReportPreview : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ReportPreview, bool>(nameof(IsOpen));

    public static readonly StyledProperty<string?> ImagePathProperty =
        AvaloniaProperty.Register<ReportPreview, string?>(nameof(ImagePath));

    public static readonly StyledProperty<bool> CanShareProperty =
        AvaloniaProperty.Register<ReportPreview, bool>(nameof(CanShare));

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ReportPreview, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<bool> HasMessageProperty =
        AvaloniaProperty.Register<ReportPreview, bool>(nameof(HasMessage));

    public static readonly StyledProperty<ICommand?> ShareCommandProperty =
        AvaloniaProperty.Register<ReportPreview, ICommand?>(nameof(ShareCommand));

    public static readonly StyledProperty<ICommand?> SaveCommandProperty =
        AvaloniaProperty.Register<ReportPreview, ICommand?>(nameof(SaveCommand));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ReportPreview, ICommand?>(nameof(CloseCommand));

    public ReportPreview()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets a value indicating whether the report covers its host screen.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the rendered PNG's path.</summary>
    public string? ImagePath
    {
        get => GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the platform offers a share sheet.</summary>
    public bool CanShare
    {
        get => GetValue(CanShareProperty);
        set => SetValue(CanShareProperty, value);
    }

    /// <summary>Gets or sets what happened to the report, shown above the actions.</summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether there is a message to show.</summary>
    public bool HasMessage
    {
        get => GetValue(HasMessageProperty);
        set => SetValue(HasMessageProperty, value);
    }

    /// <summary>Gets or sets what Compartilhar runs.</summary>
    public ICommand? ShareCommand
    {
        get => GetValue(ShareCommandProperty);
        set => SetValue(ShareCommandProperty, value);
    }

    /// <summary>Gets or sets what Salvar runs.</summary>
    public ICommand? SaveCommand
    {
        get => GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    /// <summary>Gets or sets what dismissing runs. Expected to close the preview.</summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
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
        ScreenOverlay.Current.Set(this, false);
    }
}