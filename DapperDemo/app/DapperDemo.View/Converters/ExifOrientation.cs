using System;
using System.Buffers.Binary;
using System.IO;

namespace DapperDemo.View.Converters;

/// <summary>
/// Reads the EXIF orientation tag out of a JPEG.
/// </summary>
/// <remarks>
/// Phones almost never rotate the pixels when you turn the camera; they store the sensor's own
/// orientation in this tag and leave it to the viewer. Skia — and so Avalonia's Bitmap — decodes
/// the raw pixels and ignores the tag, which is why a portrait photo comes out on its side.
/// Only JPEG is parsed: PNG has no orientation concept, and formats we cannot read fall back to
/// <see cref="Normal"/>, which is the same as doing nothing.
/// </remarks>
internal static class ExifOrientation
{
    /// <summary>Pixels are already the right way up; no transform needed.</summary>
    public const int Normal = 1;

    private const int TagOrientation = 0x0112;

    /// <summary>
    /// Reads the orientation of the image at the given path.
    /// </summary>
    /// <param name="path">Absolute path to an image file.</param>
    /// <returns>An EXIF orientation of 1 to 8, or <see cref="Normal"/> if there is no usable tag.</returns>
    public static int Read(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return ReadCore(stream);
        }
        catch (IOException)
        {
            return Normal;
        }
        catch (UnauthorizedAccessException)
        {
            return Normal;
        }
    }

    private static int ReadCore(Stream stream)
    {
        Span<byte> header = stackalloc byte[2];
        Span<byte> lengthBytes = stackalloc byte[2];

        if (stream.ReadAtLeast(header, 2, false) < 2 || header[0] != 0xFF || header[1] != 0xD8)
        {
            // Not a JPEG. Nothing else carries an orientation we act on.
            return Normal;
        }

        // Walk the marker segments looking for APP1, which is where an Exif block lives.
        while (true)
        {
            if (stream.ReadAtLeast(header, 2, false) < 2)
            {
                return Normal;
            }

            if (header[0] != 0xFF)
            {
                return Normal;
            }

            var marker = header[1];

            // Start of scan: image data follows, so there is no further metadata to find.
            if (marker == 0xDA || marker == 0xD9)
            {
                return Normal;
            }

            if (stream.ReadAtLeast(lengthBytes, 2, false) < 2)
            {
                return Normal;
            }

            // The length includes its own two bytes.
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes) - 2;
            if (segmentLength <= 0)
            {
                return Normal;
            }

            if (marker != 0xE1)
            {
                stream.Seek(segmentLength, SeekOrigin.Current);
                continue;
            }

            var segment = new byte[segmentLength];
            if (stream.ReadAtLeast(segment, segmentLength, false) < segmentLength)
            {
                return Normal;
            }

            var orientation = ReadFromApp1(segment);
            if (orientation != Normal)
            {
                return orientation;
            }

            // An APP1 that is not Exif (XMP, for instance) — keep looking.
        }
    }

    private static int ReadFromApp1(byte[] segment)
    {
        // "Exif\0\0" then a TIFF header.
        if (segment.Length < 14 ||
            segment[0] != 'E' || segment[1] != 'x' || segment[2] != 'i' || segment[3] != 'f' ||
            segment[4] != 0)
        {
            return Normal;
        }

        var tiff = segment.AsSpan(6);
        bool littleEndian;
        if (tiff[0] == 'I' && tiff[1] == 'I')
        {
            littleEndian = true;
        }
        else if (tiff[0] == 'M' && tiff[1] == 'M')
        {
            littleEndian = false;
        }
        else
        {
            return Normal;
        }

        var ifdOffset = (int)ReadUInt32(tiff.Slice(4, 4), littleEndian);
        if (ifdOffset < 8 || ifdOffset + 2 > tiff.Length)
        {
            return Normal;
        }

        var entryCount = ReadUInt16(tiff.Slice(ifdOffset, 2), littleEndian);
        var entries = tiff.Slice(ifdOffset + 2);

        for (var i = 0; i < entryCount; i++)
        {
            var offset = i * 12;
            if (offset + 12 > entries.Length)
            {
                return Normal;
            }

            var entry = entries.Slice(offset, 12);
            if (ReadUInt16(entry.Slice(0, 2), littleEndian) != TagOrientation)
            {
                continue;
            }

            // A SHORT value sits in the first two bytes of the 4-byte value field.
            var value = ReadUInt16(entry.Slice(8, 2), littleEndian);
            return value is >= 1 and <= 8 ? value : Normal;
        }

        return Normal;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, bool littleEndian) => littleEndian
        ? BinaryPrimitives.ReadUInt16LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt16BigEndian(bytes);

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, bool littleEndian) => littleEndian
        ? BinaryPrimitives.ReadUInt32LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt32BigEndian(bytes);
}