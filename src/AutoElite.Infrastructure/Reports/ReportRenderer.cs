using System.Globalization;
using AutoElite.Application.Analytics;

namespace AutoElite.Infrastructure.Reports;

/// <summary>
/// Renders reports (D-071) with libraries that any school may use: Excel workbooks with ClosedXML, PDF documents laid
/// out here and drawn with SkiaSharp in an embedded font with Cyrillic, so that the server needs no fonts of its own.
/// </summary>
internal sealed class ReportRenderer : IReportRenderer
{
    /// <inheritdoc />
    public Task<byte[]> RenderAsync(ReportDocument document, ReportFormat format, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var content = format switch
        {
            ReportFormat.Pdf => PdfReportWriter.Write(document, cancellationToken),
            ReportFormat.Excel => ExcelReportWriter.Write(document),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The report format is not defined.")
        };

        return Task.FromResult(content);
    }
}

/// <summary>Writes the values of report cells as text in the reader's culture.</summary>
internal static class ReportValues
{
    /// <summary>Determines whether the values of a kind are numbers, aligned to the right.</summary>
    /// <param name="kind">The kind of the values.</param>
    public static bool IsNumeric(ReportValueKind kind) =>
        kind is ReportValueKind.Count or ReportValueKind.Number or ReportValueKind.Money or ReportValueKind.Share;

    /// <summary>
    /// Writes a value: counts with digit grouping, numbers with up to two decimals, money with two, shares as percentages
    /// and months by name; an empty cell is empty text.
    /// </summary>
    /// <param name="value">The value, or <see langword="null"/>.</param>
    /// <param name="kind">How the value is written.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static string Format(object? value, ReportValueKind kind, IFormatProvider culture) =>
        value switch
        {
            null => string.Empty,
            DateOnly month => month.ToString("Y", culture),
            _ => kind switch
            {
                ReportValueKind.Count => Number(value).ToString("#,0", culture),
                ReportValueKind.Number => Number(value).ToString("#,0.##", culture),
                ReportValueKind.Money => Number(value).ToString("#,0.00", culture),
                ReportValueKind.Share => Number(value).ToString("#,0.##%", culture),
                _ => Convert.ToString(value, culture) ?? string.Empty
            }
        };

    /// <summary>Gets a numeric value as a decimal.</summary>
    /// <param name="value">An integer or a decimal.</param>
    public static decimal Number(object value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
}
