using AvaloniaFramework.Presentation;
using AvaloniaFramework.Presentation.UseCase;
using AvaloniaFramework.Threading;
using DapperDemo.Repository.Dapper.Services;
using DapperDemo.Viewmodel.Services;
using DapperDemo.Viewmodel.Viewmodels.Session;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace DapperDemo.Viewmodel.Viewmodels.ComplementaryViewsViewmodels;

/// <summary>
/// Ajustes: which palette the app draws in, and how big its type is.
/// </summary>
/// <remarks>
/// Both settings apply the moment they are touched rather than behind a Salvar — the screen is
/// its own preview, and a size you cannot see until you commit it is a size you cannot choose.
/// The write to disk follows the apply, so a failed write costs the preference on next launch
/// rather than the ability to change it now.
/// </remarks>
[AddINotifyPropertyChangedInterface]
public class SettingsViewModel : PresentationModelBase<Unit, Unit>
{
    private readonly DisplayPreferencesStore store;
    private readonly DisplaySettings display;

    /// <summary>Guards the reload that seeds the controls from disk against retriggering itself.</summary>
    private bool loading;

    public SettingsViewModel(CurrentView currentView, DisplayPreferencesStore store, DisplaySettings display)
    {
        ArgumentNullException.ThrowIfNull(currentView);

        this.store = store;
        this.display = display;

        BackCommand = new SynchronizedCommand(currentView.GoBack, SynchronizationBehavior.Discard, true);
        SetThemeSystemCommand = new SynchronizedCommand(() => SetTheme(AppTheme.System), SynchronizationBehavior.Discard, true);
        SetThemeLightCommand = new SynchronizedCommand(() => SetTheme(AppTheme.Light), SynchronizationBehavior.Discard, true);
        SetThemeDarkCommand = new SynchronizedCommand(() => SetTheme(AppTheme.Dark), SynchronizationBehavior.Discard, true);

        foreach (var step in TextSizeRamp.Steps)
        {
            StepOptions.Add(step);
        }

        // The slider is bound two-way, so a drag has to be picked up here rather than through a
        // command — the same shape the period pickers use.
        PropertyChanged += ApplyWhenStepChanges;
    }

    public ICommand BackCommand { get; }

    public ICommand SetThemeSystemCommand { get; }

    public ICommand SetThemeLightCommand { get; }

    public ICommand SetThemeDarkCommand { get; }

    /// <summary>Gets the six steps, so the slider knows its range and the specimens their sizes.</summary>
    public ObservableCollection<TextSizeStep> StepOptions { get; } = [];

    public AppTheme Theme { get; private set; } = DisplayPreferences.Default.Theme;

    public bool IsThemeSystem => Theme == AppTheme.System;

    public bool IsThemeLight => Theme == AppTheme.Light;

    public bool IsThemeDark => Theme == AppTheme.Dark;

    /// <summary>Gets or sets which step the slider sits on, 1 to 6.</summary>
    public double SelectedStep { get; set; } = DisplayPreferences.DefaultStep;

    /// <summary>Gets or sets a value indicating whether the system's text size drives the ramp.</summary>
    public bool FollowSystemTextSize { get; set; } = DisplayPreferences.Default.FollowSystemTextSize;

    /// <summary>
    /// Gets a value indicating whether the slider may be dragged.
    /// </summary>
    /// <remarks>
    /// While following the system it is inert but still drawn, because it is a readout of where the
    /// ramp is sitting rather than a control waiting to be enabled.
    /// </remarks>
    public bool StepIsEditable => !FollowSystemTextSize;

    /// <summary>Gets the line under the slider, naming the step and the size it sets.</summary>
    public string StepLabel
    {
        get
        {
            var step = TextSizeRamp.At(EffectiveStep);
            var size = step.Body.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

            return FollowSystemTextSize
                ? $"Seguindo o sistema — corpo {size}px"
                : $"{step.Label} — corpo {size}px";
        }
    }

    /// <summary>Gets which of the six the slider is showing, "4 de 6".</summary>
    public string StepCountLabel =>
        $"{EffectiveStep.ToString(System.Globalization.CultureInfo.InvariantCulture)} de {DisplayPreferences.StepCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>The step actually in force, which is the system's while the switch is on.</summary>
    private int EffectiveStep => FollowSystemTextSize ? display.SystemTextSizeStep : (int)Math.Round(SelectedStep);

    /// <summary>
    /// Reads the stored preference into the controls without writing it back out again.
    /// </summary>
    /// <remarks>
    /// Public because the View calls it from OnLoaded: this screen is shown by assigning
    /// CurrentView rather than being pushed, so the presenter is never RunAsync'd and
    /// OnRunStarting would never fire. Same reason the other complementary screens expose one.
    /// </remarks>
    /// <returns>A task that completes once the controls match what is on disk.</returns>
    public async Task ReloadAsync()
    {
        loading = true;

        try
        {
            var stored = await store.ReadAsync().WithSync();
            Theme = stored.Theme;
            SelectedStep = stored.TextSizeStep;
            FollowSystemTextSize = stored.FollowSystemTextSize;
        }
        finally
        {
            loading = false;
        }
    }

    protected override Task OnRunStarting(Unit input) => ReloadAsync();

    protected override Task OnRunFinishing()
    {
        PropertyChanged -= ApplyWhenStepChanges;
        return Task.CompletedTask;
    }

    private void ApplyWhenStepChanges(object? sender, PropertyChangedEventArgs e)
    {
        if (loading
            || (e.PropertyName != nameof(SelectedStep) && e.PropertyName != nameof(FollowSystemTextSize)))
        {
            return;
        }

        AppSession.FireAndForget(ApplyAsync());
    }

    private void SetTheme(AppTheme theme)
    {
        Theme = theme;
        AppSession.FireAndForget(ApplyAsync());
    }

    /// <summary>Puts the current controls into effect, then records them.</summary>
    private async Task ApplyAsync()
    {
        var preferences = new DisplayPreferences(Theme, (int)Math.Round(SelectedStep), FollowSystemTextSize);

        display.Apply(preferences);
        await store.WriteAsync(preferences).NoSync();
    }
}