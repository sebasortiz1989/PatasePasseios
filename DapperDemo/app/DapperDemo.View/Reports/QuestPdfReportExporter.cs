using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DapperDemo.Viewmodel.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Threading.Tasks;

// QuestPDF has its own Unit (a page measurement), which collides with AvaloniaFramework.Unit —
// the type this project has in a global using. Aliased rather than renamed on either side.
using PdfUnit = QuestPDF.Infrastructure.Unit;

namespace DapperDemo.View.Reports;

/// <summary>
/// Renders a <see cref="ReportDocument"/> to PDF and saves it through the platform's save dialog.
/// </summary>
/// <remarks>
/// The only class in the app that knows a PDF library exists. Styling follows the Classical
/// palette by hand rather than by resource lookup: a PDF is not part of the visual tree, so the
/// theme's brushes are not reachable from here.
/// </remarks>
public sealed class QuestPdfReportExporter : ReportExporter
{
    private const string Accent = "#B68235";
    private const string AccentDark = "#7D5411";
    private const string Ink = "#201F1D";
    private const string Muted = "#6B6A68";
    private const string Divider = "#D8D6D4";

    static QuestPdfReportExporter()
    {
        // Required before any document is generated. Community covers individuals and companies
        // under the vendor's revenue threshold — see the note in README.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public async Task<string?> ExportAsync(ReportDocument report, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(report);

        var storageProvider = GetTopLevel()?.StorageProvider;
        if (storageProvider is not { CanSave: true })
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar relatório",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = [FilePickerFileTypes.Pdf],
        }).ConfigureAwait(true);

        if (file == null)
        {
            return null;
        }

        // Rendered to bytes first: generating straight into the picked stream would leave a
        // half-written file behind if the layout threw partway through.
        var bytes = Render(report);

        var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        await using (stream.ConfigureAwait(true))
        {
            await stream.WriteAsync(bytes).ConfigureAwait(true);
        }

        return file.Name;
    }

    /// <summary>
    /// The window (desktop) or root view (mobile) the save dialog hangs off — resolved from the
    /// lifetime rather than passed in, so view models stay free of Avalonia types.
    /// </summary>
    private static TopLevel? GetTopLevel() => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
        _ => null,
    };

    private static byte[] Render(ReportDocument report) => Document.Create(container =>
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.6f, PdfUnit.Centimetre);
            page.DefaultTextStyle(text => text.FontFamily(Fonts.TimesNewRoman).FontSize(10).FontColor(Ink));

            page.Header().Element(header => ComposeHeader(header, report));
            page.Content().PaddingTop(14).Element(content => ComposeContent(content, report));
            page.Footer().Element(footer => ComposeFooter(footer, report));
        })).GeneratePdf();

    private static void ComposeHeader(IContainer container, ReportDocument report) =>
        container.Column(column =>
        {
            column.Item().Text(report.Title).FontSize(22).SemiBold().FontColor(Ink);
            column.Item().PaddingTop(2).Text(report.Subtitle).FontSize(12).FontColor(AccentDark);
            column.Item().PaddingTop(8).Width(60).BorderBottom(2).BorderColor(Accent);

            if (report.Summary.Count == 0)
            {
                return;
            }

            column.Item().PaddingTop(10).Column(summary =>
            {
                foreach (var field in report.Summary)
                {
                    summary.Item().Row(row =>
                    {
                        row.ConstantItem(110).Text(field.Label).FontSize(9).FontColor(Muted);
                        row.RelativeItem().Text(field.Value).FontSize(10);
                    });
                }
            });
        });

    private static void ComposeContent(IContainer container, ReportDocument report) =>
        container.Column(column =>
        {
            foreach (var section in report.Sections)
            {
                column.Item().PaddingTop(16).Element(item => ComposeSection(item, section));
            }
        });

    private static void ComposeSection(IContainer container, ReportSection section) =>
        container.Column(column =>
        {
            column.Item().PaddingBottom(6).Text(section.Heading).FontSize(13).SemiBold().FontColor(AccentDark);

            if (section.Fields.Count > 0)
            {
                column.Item().PaddingBottom(6).Column(fields =>
                {
                    foreach (var field in section.Fields)
                    {
                        fields.Item().PaddingVertical(1).Row(row =>
                        {
                            row.ConstantItem(110).Text(field.Label).FontSize(9).FontColor(Muted);
                            var value = row.RelativeItem().Text(field.Value)
                                .FontSize(field.Emphasised ? 13 : 10)
                                .FontColor(field.Emphasised ? AccentDark : Ink);

                            if (field.Emphasised)
                            {
                                value.SemiBold();
                            }
                        });
                    }
                });
            }

            if (section.Rows.Count == 0)
            {
                // No message either means the section is a totals-only block, such as a grand
                // total, so nothing goes where the table would have been.
                if (section.EmptyMessage.Length > 0)
                {
                    column.Item().Text(section.EmptyMessage).FontSize(10).Italic().FontColor(Muted);
                }
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // The first column carries the label and gets the slack; the rest are
                        // dates, amounts and statuses, which are all about the same width.
                        for (var i = 0; i < section.Columns.Count; i++)
                        {
                            columns.RelativeColumn(i == 0 ? 3 : 2);
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var (title, index) in section.Columns.Select((c, i) => (c, i)))
                        {
                            Align(
                                header.Cell().BorderBottom(1).BorderColor(Accent).PaddingVertical(4),
                                IsRightAligned(section, index))
                                .Text(title).FontSize(9).SemiBold().FontColor(Muted);
                        }
                    });

                    foreach (var row in section.Rows)
                    {
                        foreach (var (cell, index) in row.Cells.Select((c, i) => (c, i)))
                        {
                            Align(
                                table.Cell().BorderBottom(1).BorderColor(Divider).PaddingVertical(4),
                                IsRightAligned(section, index))
                                .Text(cell).FontSize(10);
                        }
                    }
                });
            }

            if (section.Totals.Count == 0)
            {
                return;
            }

            column.Item().PaddingTop(6).Column(totals =>
            {
                foreach (var field in section.Totals)
                {
                    totals.Item().PaddingVertical(1).Row(row =>
                    {
                        var label = row.RelativeItem().AlignRight().Text(field.Label)
                            .FontSize(field.Emphasised ? 11 : 10)
                            .FontColor(field.Emphasised ? AccentDark : Muted);

                        var value = row.ConstantItem(110).AlignRight().Text(field.Value)
                            .FontSize(field.Emphasised ? 12 : 10)
                            .FontColor(field.Emphasised ? AccentDark : Ink);

                        // SemiBold takes no arguments, so it is applied rather than passed a flag.
                        if (field.Emphasised)
                        {
                            label.SemiBold();
                            value.SemiBold();
                        }
                    });
                }
            });
        });

    private static void ComposeFooter(IContainer container, ReportDocument report) =>
        container.BorderTop(1).BorderColor(Divider).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(report.Footer).FontSize(8).FontColor(Muted);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });

    private static bool IsRightAligned(ReportSection section, int index) =>
        index < section.RightAligned.Count && section.RightAligned[index];

    private static IContainer Align(IContainer container, bool rightAligned) =>
        rightAligned ? container.AlignRight() : container;
}