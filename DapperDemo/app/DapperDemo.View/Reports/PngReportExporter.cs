using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DapperDemo.View.Services;
using DapperDemo.Viewmodel.Reports;
using DapperDemo.Viewmodel.Services;
using System;
using System.IO;
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
public sealed class PngReportExporter(FileExportDialog fileExportDialog) : ReportExporter
{
    /// <summary>
    /// Wider than the app's 720 design canvas. This is a document to be shared and zoomed, not a
    /// phone screen, and a six-column table does not fit in 720 without the columns colliding.
    /// </summary>
    /// <remarks>
    /// Widened from 980 when hotel rows gained their breakdown lines: the money column grew to fit
    /// "3 diárias × R$ 120,00", and the slack came out of the one column that gives it up — the
    /// star one holding the dog's name, which started breaking "Maximiliano" across two lines.
    /// </remarks>
    private const double Width = 1120;

    private const double Padding = 40;

    /// <summary>Gap between table columns. Without one a right-aligned cell touches its neighbour.</summary>
    private const double ColumnGap = 22;

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

    /// <inheritdoc/>
    public Task<string?> RenderAsync(ReportDocument report, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(report);

        // A folder of our own inside the temporary one, so cleaning up a stale preview cannot
        // reach anything else the system keeps there.
        var folder = Path.Combine(Path.GetTempPath(), "dapperdemo-reports");
        var path = Path.Combine(folder, Path.GetFileName(suggestedFileName) + ".png");

        try
        {
            Directory.CreateDirectory(folder);

            using var bitmap = Render(report);
            var file = File.Create(path);

            using (file)
            {
                bitmap.Save(file, new PngBitmapEncoderOptions());
            }

            return Task.FromResult<string?>(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> SaveAsync(string renderedPath, string suggestedFileName, Func<string, Task<bool>> confirmReplace)
    {
        // Where the file goes, whether an existing one may be replaced, and how the platform is
        // best asked, all belong to the dialog — this only knows how to draw.
        var target = await fileExportDialog
            .CreateAsync(suggestedFileName, ExportFileKind.Png, confirmReplace)
            .ConfigureAwait(true);

        if (target == null)
        {
            return null;
        }

        try
        {
            var source = File.OpenRead(renderedPath);

            await using (source.ConfigureAwait(true))
            await using (target.Content.ConfigureAwait(true))
            {
                await source.CopyToAsync(target.Content).ConfigureAwait(true);
            }

            return target.Name;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine(e);
            return null;
        }
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

        // Only the first column is proportional; the rest size to their content. Star columns gave
        // every one an equal slice regardless of what it held, which is what wrapped "04/08/2026,
        // 09:00" onto three lines while the status columns sat half empty.
        for (var i = 0; i < section.Columns.Count; i++)
        {
            grid.ColumnDefinitions.Add(i == 0
                ? new ColumnDefinition(1, GridUnitType.Star)
                : new ColumnDefinition(GridLength.Auto));
        }

        for (var i = 0; i <= section.Rows.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        var last = section.Columns.Count - 1;

        for (var column = 0; column < section.Columns.Count; column++)
        {
            var header = Text(section.Columns[column], 20, Muted, Body, FontWeight.SemiBold);

            // The gap lives on every column but the last, so a right-aligned value can never touch
            // the heading beside it — which is what ran "Valor" into "Execução".
            header.Margin = new Thickness(0, 0, column == last ? 0 : ColumnGap, 10);
            header.TextWrapping = TextWrapping.NoWrap;
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
                var cell = BuildCell(
                    cells[column],
                    section.Rows[row].DetailAt(column),
                    IsRightAligned(section, column),
                    column == 0);

                cell.Margin = new Thickness(0, 9, column == last ? 0 : ColumnGap, 9);
                Grid.SetColumn(cell, column);
                Grid.SetRow(cell, row + 1);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    /// <summary>
    /// One cell: its value, and under it the small print the row attached to that column, if any.
    /// </summary>
    /// <remarks>
    /// Stacked text blocks rather than one block with inlines, so the detail can carry its own size
    /// and colour. A cell with no detail stays a bare <see cref="TextBlock"/> — the table's column
    /// widths come from measuring these, and an extra panel around every cell would be one more
    /// thing between the text and the width it asks for.
    /// </remarks>
    private static Control BuildCell(string text, string detail, bool rightAligned, bool wraps)
    {
        var alignment = rightAligned ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        var main = Text(text, 23, Ink);

        // Auto columns measure at their unwrapped width, so only the first — the star one that
        // absorbs the slack — is allowed to wrap.
        main.TextWrapping = wraps ? TextWrapping.Wrap : TextWrapping.NoWrap;
        main.HorizontalAlignment = alignment;

        if (detail.Length == 0)
        {
            return main;
        }

        var stack = new StackPanel { HorizontalAlignment = alignment };
        stack.Children.Add(main);

        foreach (var line in detail.Split('\n'))
        {
            var note = Text(line, 19, Muted);
            note.TextWrapping = TextWrapping.NoWrap;
            note.HorizontalAlignment = alignment;
            note.Margin = new Thickness(0, 3, 0, 0);
            stack.Children.Add(note);
        }

        return stack;
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