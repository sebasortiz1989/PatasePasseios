using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace DapperDemo.View.Converters;

/// <summary>
/// Turns a stored photo's absolute path into something an Image can show, the right way up.
/// </summary>
/// <remarks>
/// Avalonia's string-to-image conversion only covers avares:// URIs resolved when the XAML is
/// parsed; a path arriving through a binding at runtime needs this. Decoded bitmaps are cached
/// per path because the dogs list re-binds every row on each refresh, and decoding — let alone
/// re-orienting — a camera photo per row per refresh is what makes a list scroll badly.
/// </remarks>
public sealed class ImagePathConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();

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

    /// <summary>
    /// Maps source pixels onto the upright image for a given EXIF orientation, and reports the
    /// size that upright image needs. The quarter-turn cases swap width and height.
    /// </summary>
    private static (Matrix Transform, PixelSize Size) TransformFor(int orientation, PixelSize source)
    {
        var w = source.Width;
        var h = source.Height;
        var turned = new PixelSize(h, w);

        return orientation switch
        {
            2 => (new Matrix(-1, 0, 0, 1, w, 0), source),      // mirrored horizontally
            3 => (new Matrix(-1, 0, 0, -1, w, h), source),     // upside down
            4 => (new Matrix(1, 0, 0, -1, 0, h), source),      // mirrored vertically
            5 => (new Matrix(0, 1, 1, 0, 0, 0), turned),       // transposed
            6 => (new Matrix(0, 1, -1, 0, h, 0), turned),      // quarter turn clockwise
            7 => (new Matrix(0, -1, -1, 0, h, w), turned),     // transversed
            8 => (new Matrix(0, -1, 1, 0, 0, w), turned),      // quarter turn anticlockwise
            _ => (Matrix.Identity, source),
        };
    }

    private static Bitmap? Decode(string path)
    {
        try
        {
            Bitmap decoded;

            // Read through a stream that is closed straight away rather than handing the file to
            // the Bitmap: a photo the user then replaces has to be deletable while the app runs.
            using (var stream = File.OpenRead(path))
            {
                decoded = new Bitmap(stream);
            }

            var orientation = ExifOrientation.Read(path);
            if (orientation == ExifOrientation.Normal)
            {
                return decoded;
            }

            using (decoded)
            {
                return Reorient(decoded, orientation);
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            // An unreadable or unsupported image. A blank circle beats an exception thrown from
            // inside a data template, which takes the whole list down with it.
            return null;
        }
    }

    private static RenderTargetBitmap Reorient(Bitmap source, int orientation)
    {
        var (transform, size) = TransformFor(orientation, source.PixelSize);

        // Rendered at 96 dpi so one source pixel is one destination pixel and the maths above
        // needs no scaling factor. The display sites all size the image themselves.
        var target = new RenderTargetBitmap(size);
        using (var context = target.CreateDrawingContext())
        using (context.PushTransform(transform))
        {
            context.DrawImage(source, new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height));
        }

        return target;
    }
}