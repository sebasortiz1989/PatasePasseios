using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvaloniaFramework.Threading;
using System;

namespace DapperDemo.View.Imaging;

/// <summary>
/// Fills an <see cref="Image"/> from a photo on disk without decoding it on the UI thread.
/// </summary>
/// <remarks>
/// <para>
/// This replaces binding <see cref="Image.Source"/> through a converter. A converter has to return
/// the bitmap there and then, which means the file is opened and decoded inside the layout pass —
/// on the UI thread, while the list is scrolling. Virtualization decides how many rows do that;
/// it cannot stop the ones that do from blocking the frame.
/// </para>
/// <para>
/// Usage: <c>imaging:ImageLoader.Path="{Binding ImagePath}"</c>, with
/// <c>imaging:ImageLoader.DecodeWidth="192"</c> where the display size is smaller than the default.
/// </para>
/// </remarks>
public static class ImageLoader
{
    /// <summary>The photo's absolute path. Null or empty clears the image.</summary>
    public static readonly AttachedProperty<string?> PathProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>("Path", typeof(ImageLoader));

    /// <summary>Physical pixels to decode to. Defaults to <see cref="PhotoCache.DefaultDecodeWidth"/>.</summary>
    public static readonly AttachedProperty<int> DecodeWidthProperty =
        AvaloniaProperty.RegisterAttached<Image, int>("DecodeWidth", typeof(ImageLoader), PhotoCache.DefaultDecodeWidth);

    /// <summary>
    /// Counts the requests made of one Image, so a decode that lands late can tell whether it is
    /// still wanted.
    /// </summary>
    /// <remarks>
    /// A virtualizing panel recycles a row's controls onto a different dog as it scrolls. Without
    /// this, a slow decode started for the row that just scrolled away would arrive and paint that
    /// dog's photo onto the row now showing another one.
    /// </remarks>
    private static readonly AttachedProperty<int> GenerationProperty =
        AvaloniaProperty.RegisterAttached<Image, int>("Generation", typeof(ImageLoader));

    static ImageLoader()
    {
        PathProperty.Changed.AddClassHandler<Image>((image, _) => Refresh(image));
        DecodeWidthProperty.Changed.AddClassHandler<Image>((image, _) => Refresh(image));
    }

    /// <summary>Sets the photo path an <see cref="Image"/> shows.</summary>
    public static void SetPath(Image image, string? value) => image.SetValue(PathProperty, value);

    /// <summary>Gets the photo path an <see cref="Image"/> shows.</summary>
    public static string? GetPath(Image image) => image.GetValue(PathProperty);

    /// <summary>Sets the width, in physical pixels, an <see cref="Image"/>'s photo is decoded to.</summary>
    public static void SetDecodeWidth(Image image, int value) => image.SetValue(DecodeWidthProperty, value);

    /// <summary>Gets the width, in physical pixels, an <see cref="Image"/>'s photo is decoded to.</summary>
    public static int GetDecodeWidth(Image image) => image.GetValue(DecodeWidthProperty);

    private static async void Refresh(Image image)
    {
        var generation = image.GetValue(GenerationProperty) + 1;
        image.SetValue(GenerationProperty, generation);

        var path = image.GetValue(PathProperty);
        var width = image.GetValue(DecodeWidthProperty);

        if (string.IsNullOrWhiteSpace(path))
        {
            image.Source = null;
            return;
        }

        // Already decoded at this size: assign it in this frame, so a row scrolled back into view
        // does not blank and refill.
        if (PhotoCache.Peek(path, width) is { } cached)
        {
            image.Source = cached;
            return;
        }

        // Blank while the decode runs, so a recycled row never shows the previous dog's photo
        // under the new dog's name.
        image.Source = null;

        Bitmap? bitmap;

        try
        {
            bitmap = await PhotoCache.LoadAsync(path, width).WithSync();
        }
#pragma warning disable CA1031 // Deliberately general — see the comment in the block.
        catch (Exception)
#pragma warning restore CA1031
        {
            // This method is the async void boundary, so anything escaping it takes the process
            // down rather than surfacing somewhere it can be handled. PhotoCache already returns
            // null for the file being missing or unreadable; what is left is the unexpected — a
            // full-size decode running out of memory on a phone, most plausibly. A dog with no
            // photo showing beats the app closing.
            return;
        }

        if (image.GetValue(GenerationProperty) == generation)
        {
            image.Source = bitmap;
        }
    }
}