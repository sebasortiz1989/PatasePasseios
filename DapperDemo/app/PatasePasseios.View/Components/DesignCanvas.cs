using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace PatasePasseios.View.Components;

/// <summary>
/// Hosts a screen authored against the fixed design canvas and scales it to the device.
/// <para>
/// This replaces <see cref="Viewbox"/> as the root of every screen. A Viewbox fits the whole canvas
/// uniformly, so a device whose aspect ratio is not the design's 720:1560 gets bars — visibly black
/// ones, because the framework's ShellView paints black behind the app. Here the scale is taken from
/// the width alone, and the leftover height is handed to the child as extra canvas instead: the
/// child is measured at <see cref="DesignWidth"/> by however many design units of height the device
/// actually affords. Screens are built around a ScrollViewer, so they absorb that difference.
/// </para>
/// <para>
/// On a display too wide for that — a desktop window, a tablet — scaling by width would blow the
/// canvas up far past the height available. There the height caps the scale instead and the canvas
/// is centred horizontally, which is exactly what the Viewbox used to do.
/// </para>
/// </summary>
public sealed class DesignCanvas : Decorator
{
    public static readonly StyledProperty<double> DesignWidthProperty =
        AvaloniaProperty.Register<DesignCanvas, double>(nameof(DesignWidth), 720d);

    public static readonly StyledProperty<double> DesignHeightProperty =
        AvaloniaProperty.Register<DesignCanvas, double>(nameof(DesignHeight), 1560d);

    private readonly ScaleTransform transform = new();

    public DesignCanvas()
    {
        // The child is arranged at design size and only then scaled, so it overhangs these bounds
        // whenever the scale is below 1.
        ClipToBounds = true;
    }

    /// <summary>Gets or sets the width, in design units, the child is laid out at. Always honoured.</summary>
    public double DesignWidth
    {
        get => GetValue(DesignWidthProperty);
        set => SetValue(DesignWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height, in design units, the child is laid out at when the device affords
    /// nothing more. Taller devices give the child more than this; none give it less.
    /// </summary>
    public double DesignHeight
    {
        get => GetValue(DesignHeightProperty);
        set => SetValue(DesignHeightProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        var scale = ScaleFor(availableSize);
        var canvas = CanvasFor(availableSize, scale);
        child.Measure(canvas);

        // Report the space we were given, not what the child wanted. A canvas fills its host by
        // definition, and reporting the child's scaled height was what produced the short page:
        // a measure pass with no height — which Android's soft-keyboard handling produces — makes
        // CanvasFor fall back to DesignHeight, so the canvas asked for 1560 units' worth and the
        // rest of the screen stayed the shell's black. Only an unbounded axis falls back.
        return new Size(
            double.IsInfinity(availableSize.Width) ? canvas.Width * scale : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? canvas.Height * scale : availableSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
        {
            return finalSize;
        }

        var scale = ScaleFor(finalSize);
        var canvas = CanvasFor(finalSize, scale);

        // Measured again against the canvas it is about to be arranged on. Measure may have run
        // with no height and sized the child for 1560 units; arranging it taller without a second
        // measure leaves its content laid out for a canvas that is no longer the one it is on.
        child.Measure(canvas);

        // Arranging at full design size and scaling afterwards — the mechanism Viewbox uses — keeps
        // every measurement inside the child in design units, so the authored pixel values still
        // mean what they meant on the 720-wide canvas.
        this.transform.ScaleX = scale;
        this.transform.ScaleY = scale;
        child.RenderTransformOrigin = RelativePoint.TopLeft;
        child.RenderTransform = this.transform;

        var left = (finalSize.Width - (canvas.Width * scale)) / 2;
        child.Arrange(new Rect(left, 0, canvas.Width, canvas.Height));
        return finalSize;
    }

    /// <summary>
    /// Works out the display scale. Width drives it, except where that would push the canvas past
    /// the height available, in which case height caps it.
    /// </summary>
    private double ScaleFor(Size available)
    {
        if (double.IsInfinity(available.Width) || DesignWidth <= 0)
        {
            return 1;
        }

        var scale = available.Width / DesignWidth;
        if (!double.IsInfinity(available.Height) && DesignHeight > 0)
        {
            scale = Math.Min(scale, available.Height / DesignHeight);
        }

        return scale > 0 ? scale : 1;
    }

    /// <summary>Works out the canvas the child is laid out on, in design units.</summary>
    private Size CanvasFor(Size available, double scale)
    {
        var height = double.IsInfinity(available.Height)
            ? DesignHeight
            : Math.Max(DesignHeight, available.Height / scale);

        return new Size(DesignWidth, height);
    }
}