using Avalonia;
using AvaloniaFramework.Controls;
using AvaloniaFramework.Hosting;
using DapperDemo.View.Components;
using DapperDemo.Viewmodel.Viewmodels;
using DapperDemo.Viewmodel.Viewmodels.NavigationViewsViewmodels;
using System;

namespace DapperDemo.View.Views.NavigationViews;

public partial class MainView : PresenterUserControl<MainViewModel, Unit, Unit>
{
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Keeps the navigation bar out of the way of anything covering the screen.
    /// </summary>
    /// <remarks>
    /// Chrome, not business logic — the bar belongs to this screen and nothing else can reach it.
    /// A tab's dialogs and full-screen images are drawn inside the content control below the bar,
    /// so hiding it is the only way they can cover the whole screen. See <see cref="ScreenOverlay"/>
    /// for why the alternatives do not work here.
    /// </remarks>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        ScreenOverlay.Current.CoveredChanged += OnCoveredChanged;
        ApplyCovered();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Signing out and back in builds a new MainView; without this the old one stays subscribed
        // to a singleton for the rest of the session.
        ScreenOverlay.Current.CoveredChanged -= OnCoveredChanged;
    }

    private void OnCoveredChanged(object? sender, EventArgs e) => ApplyCovered();

    private void ApplyCovered() => NavigationBar.IsVisible = !ScreenOverlay.Current.IsCovered;
}