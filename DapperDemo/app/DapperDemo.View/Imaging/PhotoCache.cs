using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaFramework.Threading;
using DapperDemo.View.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DapperDemo.View.Imaging;

/// <summary>
/// Decodes stored photos to the size a display site asks for, and keeps a bounded number of the
/// results.
/// </summary>
/// <remarks>
/// The reading of the file and the decode happen on the thread pool. A virtualized list realizes a
/// row the instant it scrolls into view, and opening a camera JPEG and decoding it there — inside
/// the layout pass, on the UI thread — is what makes the list stutter however well it virtualizes.
/// Only the EXIF re-orientation stays on the UI thread, because it draws through a render target;
/// it runs on the already-downscaled bitmap and costs a fraction of the decode.
/// </remarks>
internal static class PhotoCache
{
    /// <summary>
    /// Physical pixels a photo is decoded to when the display site does not say.
    /// </summary>
    /// <remarks>
    /// Sized for the largest place a photo appears at its display size — 168 canvas units on the
    /// detail and profile screens, which is under 300 physical pixels even at 3×. The full-size
    /// viewer asks for <see cref="FullSize"/> instead.
    /// </remarks>
    internal const int DefaultDecodeWidth = 512;

    /// <summary>
    /// Asks for the photo at the resolution it was stored at, with no downscale.
    /// </summary>
    /// <remarks>
    /// Only the full-size viewer uses this. Photos are still saved at whatever the camera produced,
    /// so one of these can be tens of megabytes decoded — which is why it is one image on its own
    /// screen and never a list row.
    /// </remarks>
    internal const int FullSize = 0;

    /// <summary>
    /// How many decoded photos are held before the least recently used one is dropped.
    /// </summary>
    /// <remarks>
    /// A 192px avatar is roughly 150 KB decoded and a 512px photo roughly 1 MB, so this bounds the
    /// cache at a few tens of megabytes. Unbounded — which is what it was — scrolling a long list
    /// once pins every photo it passed for the lifetime of the app.
    /// </remarks>
    private const int Capacity = 64;

    private static readonly Lock Gate = new();

    private static readonly Dictionary<(string Path, int Width), LinkedListNode<Entry>> Index = [];

    /// <summary>Most recently used first, so the last node is the one eviction takes.</summary>
    private static readonly LinkedList<Entry> Order = new();

    /// <summary>
    /// Returns a photo that has already been decoded at this width, or null when it has not.
    /// </summary>
    /// <remarks>
    /// This is what lets a row scrolled back into view draw its photo in the same frame, instead of
    /// showing the blank-then-fill an asynchronous load would give it every time.
    /// </remarks>
    internal static Bitmap? Peek(string path, int width)
    {
        lock (Gate)
        {
            if (!Index.TryGetValue((path, width), out var node))
            {
                return null;
            }

            Order.Remove(node);
            Order.AddFirst(node);
            return node.Value.Image;
        }
    }

    /// <summary>
    /// Decodes a photo at the given width, off the UI thread, and caches it.
    /// </summary>
    /// <param name="path">The photo's absolute path.</param>
    /// <param name="width">
    /// Physical pixels to decode to, or <see cref="FullSize"/> for the stored resolution.
    /// </param>
    /// <returns>The decoded photo, or null when the file is missing or unreadable.</returns>
    internal static async Task<Bitmap?> LoadAsync(string path, int width)
    {
        if (Peek(path, width) is { } cached)
        {
            return cached;
        }

        var decoded = await Task.Run(() => Decode(path, width)).WithSync();
        if (decoded is null)
        {
            return null;
        }

        var (raw, orientation) = decoded.Value;
        Bitmap upright;

        if (orientation == ExifOrientation.Normal)
        {
            upright = raw;
        }
        else
        {
            using (raw)
            {
                // CA2000: ownership passes to the cache, which holds the bitmap for as long as any
                // Image may still be drawing it — see the note in Store.
#pragma warning disable CA2000
                upright = Reorient(raw, orientation);
#pragma warning restore CA2000
            }
        }

        Store(path, width, upright);
        return upright;
    }

    private static void Store(string path, int width, Bitmap image)
    {
        lock (Gate)
        {
            var key = (path, width);
            if (Index.TryGetValue(key, out var existing))
            {
                Order.Remove(existing);
                Index.Remove(key);
            }

            Index[key] = Order.AddFirst(new Entry(key, image));

            while (Order.Count > Capacity)
            {
                var oldest = Order.Last!;
                Order.RemoveLast();
                Index.Remove(oldest.Value.Key);

                // Evicted without being disposed on purpose. An Image control still on screen may
                // hold this bitmap, and disposing one that is being drawn tears down the surface
                // underneath it. Dropping the reference lets the finalizer reclaim it once nothing
                // is showing it.
            }
        }
    }

    /// <summary>
    /// Reads and decodes the file. Runs on the thread pool, so it touches no Avalonia visual.
    /// </summary>
    private static (Bitmap Image, int Orientation)? Decode(string path, int width)
    {
        try
        {
            Bitmap decoded;

            // Read through a stream that is closed straight away rather than handing the file to
            // the Bitmap: a photo the user then replaces has to be deletable while the app runs.
            using (var stream = File.OpenRead(path))
            {
                // DecodeToWidth scales during the decode, so the full-size image never exists in
                // memory at all. A source narrower than the request is scaled up, which costs
                // nothing worth measuring at these sizes.
                decoded = width == FullSize
                    ? new Bitmap(stream)
                    : Bitmap.DecodeToWidth(stream, width);
            }

            return (decoded, ExifOrientation.Read(path));
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
            // An unreadable or unsupported image. A blank circle beats an exception surfacing from
            // a data template, which takes the whole list down with it.
            return null;
        }
    }

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

    private readonly record struct Entry((string Path, int Width) Key, Bitmap Image);
}