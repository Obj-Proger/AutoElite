using AutoElite.Application.Analytics;
using SkiaSharp;

namespace AutoElite.Infrastructure.Reports;

/// <summary>
/// Writes a report as a PDF document: lays it out with <see cref="PdfReportLayout"/> and draws the pages with SkiaSharp,
/// which embeds the used part of the font. The document carries the title of the report.
/// </summary>
internal static class PdfReportWriter
{
    private const string Creator = "AutoElite";

    private const float RuleWidth = 0.5f;

    /// <summary>Writes the report.</summary>
    /// <param name="document">The report.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation, checked between pages.</param>
    /// <returns>The content of the document.</returns>
    public static byte[] Write(ReportDocument document, CancellationToken cancellationToken)
    {
        using var fonts = new PdfFonts();
        var layout = PdfReportLayout.Lay(document, fonts);

        var metadata = SKDocumentPdfMetadata.Default;
        metadata.Title = document.Title;
        metadata.Creator = Creator;
        metadata.Producer = Creator;

        using var content = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(content, metadata)
            ?? throw new InvalidOperationException("SkiaSharp could not create a PDF document."))
        {
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.StrokeWidth = RuleWidth;
            foreach (var page in layout.Pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var canvas = pdf.BeginPage(layout.PageSize.Width, layout.PageSize.Height);
                Draw(canvas, page, fonts, paint);
                pdf.EndPage();
            }

            pdf.Close();
        }

        return content.ToArray();
    }

    private static void Draw(SKCanvas canvas, PdfPage page, PdfFonts fonts, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;
        foreach (var fill in page.Fills)
        {
            paint.Color = fill.Color;
            canvas.DrawRect(fill.Area, paint);
        }

        paint.Style = SKPaintStyle.Stroke;
        foreach (var rule in page.Rules)
        {
            paint.Color = rule.Color;
            canvas.DrawLine(rule.Left, rule.Y, rule.Right, rule.Y, paint);
        }

        paint.Style = SKPaintStyle.Fill;
        foreach (var text in page.Texts)
        {
            paint.Color = text.Color;
            canvas.DrawText(text.Text, text.X, text.Baseline, text.Align, fonts.Get(text.Style, text.Size), paint);
        }
    }
}
