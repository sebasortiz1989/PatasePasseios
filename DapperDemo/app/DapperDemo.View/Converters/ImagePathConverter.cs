using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace DapperDemo.View.Converters;

/// <summary>
/// Turns a stored photo's absolute path into something an Image can show.
/// </summary>
/// <remarks>
/// Avalonia's string-to-image conversion only covers avares:// URIs resolved when the XAML is
/// parsed; a path arriving through a binding at runtime needs this. Decoded bitmaps are cached
/// per path because the dogs list re-binds every row on each refresh, and decoding a camera
/// photo per row per refresh is what makes a list scroll badly.
/// </remarks>
public sealed class ImagePathConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        return Cache.GetOrAdd(path, Decode);
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Photos are chosen through the picker, not typed back through the binding.");

    private static Bitmap Decode(string path)
    {
        // Read through a stream that is closed straight away rather than handing the file to the
        // Bitmap: a photo the user then replaces has to be deletable while the app still runs.
        using var stream = File.OpenRead(path);
        return new Bitmap(stream);
    }
}