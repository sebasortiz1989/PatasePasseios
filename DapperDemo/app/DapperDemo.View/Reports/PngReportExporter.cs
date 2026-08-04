using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DapperDemo.View.Services;
using DapperDemo.Viewmodel.Reports;
using System;
using System.Threading.Tasks;

namespace DapperDemo.View.Reports;

/// <summary>
/// Draws a <see cref="ReportDocument"/> as a PNG and saves it through the platform's save dialog.
/// </summary>
/// <remarks>
/// <para>
/// The image is built from ordinary Avalonia controls, laid out off-screen and rendered with
/// <see cref="RenderTargetBitmap"/>. That keeps the whole feature inside Avalonia — no PDF or
/// imaging library, nothing extra to ship, and it works the same on desktop and on a phone.
/// </para>
/// <para>
/// Colours and fonts are set explicitly rather than through StaticResource. The tree built here
/// is never attached to a window, so the application's styles never reach it and a theme lookup
/// would come back empty.
/// </para>
/// </remarks>
public sealed class PngReportExporter : ReportExporter
{
    private const double Width = 720;
    private const double Padding = 40;

    /// <summary>Rendered at twice the layout size, so the text is still sharp when zoomed into.</summary>
    private const double Scale = 2;

    private static readonly IBrush Ink = new SolidColorBrush(Color.Parse("#201F1D"));
    private static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#8C201F1D"));
    private static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#B68235"));
    private static readonly IBrush AccentDark = new SolidColorBrush(Color.Parse("#7D5411"));
    private static readonly IBrush Divider = new SolidColorBrush(Color.Parse("#29201F1D"));
    private static readonly IBrush Background = new SolidColorBrush(Color.Parse("#F3F2F2"));

    private static readonly FontFamily Heading = new("Times New Roman,Georgia,serif");
    private static readonly FontFamily Body = new("Georgia,Times New Roman,serif");

    private static readonly FilePickerFileType PngFileType = new("Imagem (.png)")
    {
        Patterns = ["*.png"],
        MimeTypes = ["image/png"],
        AppleUniformTypeIdentifiers = ["public.png"],
    };

    /// <inheritdoc/>
    public async Task<string?> ExportAsync(ReportDocument report, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(report);

        var storageProvider = ShellTopLevel.Resolve()?.StorageProvider;
        if (storageProvider is not { CanSave: true })
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar resumo",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "png",
            FileTypeChoices = [PngFileType],
            ShowOverwritePrompt = true,
        }).ConfigureAwait(true);

        if (file == null)
        {
            return null;
        }

        using var bitmap = Render(report);

        var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        await using (stream.ConfigureAwait(true))
        {
            bitmap.Save(stream, new PngBitmapEncoderOptions());
        }

        return file.Name;
    }

    private static TextBlock Text(
        string text,
        double size,
        IBrush brush,
        FontFamily? family = null,
        FontWeight weight = FontWeight.Normal,
        FontStyle style = FontStyle.Normal) => new()
        {
            Text = text,
            FontSize = size,
            Foreground = brush,
            FontFamily = family ?? Body,
            FontWeight = weight,
            FontStyle = style,
            TextWrapping = TextWrapping.Wrap,
        };

    private static Border Rule(double thickness, IBrush brush, double top) => new Border
    {
        Height = thickness,
        Background = brush,
        Margin = new Thickness(0, top, 0, 0),
    };

    /// <summary>A label on the left and its value on the right, the shape most of the report uses.</summary>
    private static Grid LabelledValue(ReportField field, bool rightAlignLabel)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 3, 0, 3),
        };

        var label = Text(field.Label, field.Emphasised ? 24 : 22, field.Emphasised ? AccentDark : Muted);
        label.VerticalAlignment = VerticalAlignment.Center;
        if (rightAlignLabel)
        {
            label.HorizontalAlignment = HorizontalAlignment.Right;
            label.Margin = new Thickness(0, 0, 16, 0);
        }

        var value = Text(
            field.Value,
            field.Emphasised ? 30 : 24,
            field.Emphasised ? AccentDark : Ink,
            field.Emphasised ? Heading : Body,
            field.Emphasised ? FontWeight.SemiBold : FontWeight.Normal);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(value, 1);

        grid.Children.Add(label);
        grid.Children.Add(value);
        return grid;
    }

    private static Grid BuildTable(ReportSection section)
    {
        var grid = new Grid();

        // The first column carries the name and takes the slack; the rest are dates, amounts and
        // statuses, which are all about the same width.
        for (var i = 0; i < section.Columns.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(i == 0 ? 3 : 2, GridUnitType.Star));
        }

        for (var i = 0; i <= section.Rows.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var column = 0; column < section.Columns.Count; column++)
        {
            var header = Text(section.Columns[column], 20, Muted, Body, FontWeight.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 8);
            header.HorizontalAlignment = IsRightAligned(section, column) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            Grid.SetColumn(header, column);
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
        }

        for (var row = 0; row < section.Rows.Count; row++)
        {
            var cells = section.Rows[row].Cells;
            for (var column = 0; column < cells.Count && column < section.Columns.Count; column++)
            {
                var cell = Text(cells[column], 23, Ink);
                cell.Margin = new Thickness(0, 8, 0, 8);
                cell.HorizontalAlignment = IsRightAligned(section, column) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                Grid.SetColumn(cell, column);
                Grid.SetRow(cell, row + 1);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    private static bool IsRightAligned(ReportSection section, int index) =>
        index < section.RightAligned.Count && section.RightAligned[index];

    private static StackPanel BuildSection(ReportSection section)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 28, 0, 0) };

        stack.Children.Add(Text(section.Heading, 28, AccentDark, Heading, FontWeight.SemiBold));

        foreach (var field in section.Fields)
        {
            stack.Children.Add(LabelledValue(field, false));
        }

        if (section.Rows.Count > 0)
        {
            stack.Children.Add(Rule(2, Accent, 10));
            stack.Children.Add(BuildTable(section));
        }
        else if (section.EmptyMessage.Length > 0)
        {
            stack.Children.Add(Text(section.EmptyMessage, 22, Muted, Body, FontWeight.Normal, FontStyle.Italic));
        }

        if (section.Totals.Count > 0)
        {
            stack.Children.Add(Rule(2, Divider, 10));
            foreach (var total in section.Totals)
            {
                stack.Children.Add(LabelledValue(total, true));
            }
        }

        return stack;
    }

    private static Border BuildRoot(ReportDocument report)
    {
        var body = new StackPanel();

        body.Children.Add(Text(report.Title, 48, Ink, Heading, FontWeight.Bold));
        body.Children.Add(Text(report.Subtitle, 26, AccentDark));

        var underline = new Border
        {
            Height = 3,
            Width = 80,
            Background = Accent,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 14, 0, 0),
        };
        body.Children.Add(underline);

        if (report.Summary.Count > 0)
        {
            var summary = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
            foreach (var field in report.Summary)
            {
                summary.Children.Add(LabelledValue(field, false));
            }

            body.Children.Add(summary);
        }

        foreach (var section in report.Sections)
        {
            body.Children.Add(BuildSection(section));
        }

        if (report.Footer.Length > 0)
        {
            body.Children.Add(Rule(2, Divider, 28));
            var footer = Text(report.Footer, 19, Muted);
            footer.Margin = new Thickness(0, 10, 0, 0);
            body.Children.Add(footer);
        }

        return new Border
        {
            Background = Background,
            Padding = new Thickness(Padding),
            Width = Width,
            Child = body,
        };
    }

    private static RenderTargetBitmap Render(ReportDocument report)
    {
        var root = BuildRoot(report);

        // Laid out by hand: the tree is never attached to a window, so nothing else will do it,
        // and Render draws whatever the last arrange pass decided.
        root.Measure(new Size(Width, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, Width, root.DesiredSize.Height));

        var height = Math.Max(root.Bounds.Height, root.DesiredSize.Height);
        var pixelSize = new PixelSize((int)Math.Ceiling(Width * Scale), (int)Math.Ceiling(height * Scale));

        // The dpi does the scaling: the tree is still laid out in its own units, and the renderer
        // draws it at twice the resolution.
        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * Scale, 96 * Scale));
        bitmap.Render(root);
        return bitmap;
    }
}