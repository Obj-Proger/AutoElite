using AutoElite.Application.Analytics;
using ClosedXML.Excel;

namespace AutoElite.Infrastructure.Reports;

/// <summary>
/// Writes a report as an Excel workbook of one worksheet: the title, the lines under it, then the tables one under
/// another. Values stay numbers and dates, with number formats that Excel shows in the reader's own settings, so that the
/// workbook can be calculated further; months are dates of their first day shown as month and year.
/// </summary>
/// <remarks>
/// Column widths are counted from the length of the texts rather than measured with fonts, so that writing needs no
/// fonts on the server.
/// </remarks>
internal static class ExcelReportWriter
{
    // The longest worksheet name Excel accepts.
    private const int MaxSheetNameLength = 31;

    private const string FallbackSheetName = "Report";

    private const double MinColumnWidth = 8;

    private const double MaxColumnWidth = 60;

    private const double TitleFontSize = 14;

    private const double TableTitleFontSize = 12;

    private static readonly char[] _sheetNameForbidden = ['[', ']', ':', '*', '?', '/', '\\'];

    private static readonly XLColor _headerFill = XLColor.FromArgb(0xEE, 0xEE, 0xEE);

    private static readonly XLColor _detailsColor = XLColor.FromArgb(0x61, 0x61, 0x61);

    /// <summary>Writes the report.</summary>
    /// <param name="document">The report.</param>
    /// <returns>The content of the workbook.</returns>
    public static byte[] Write(ReportDocument document)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = document.Title;
        var sheet = workbook.AddWorksheet(SheetName(document.Title));
        var widths = new Dictionary<int, int>();

        var row = 1;
        var title = sheet.Cell(row++, 1);
        title.Value = document.Title;
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = TitleFontSize;
        foreach (var detail in document.Details)
        {
            var cell = sheet.Cell(row++, 1);
            cell.Value = detail;
            cell.Style.Font.FontColor = _detailsColor;
        }

        foreach (var table in document.Tables)
        {
            row++;
            if (table.Title is not null)
            {
                var tableTitle = sheet.Cell(row++, 1);
                tableTitle.Value = table.Title;
                tableTitle.Style.Font.Bold = true;
                tableTitle.Style.Font.FontSize = TableTitleFontSize;
            }

            for (var column = 0; column < table.Columns.Count; column++)
            {
                var header = sheet.Cell(row, column + 1);
                header.Value = table.Columns[column].Header;
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = _headerFill;
                if (ReportValues.IsNumeric(table.Columns[column].Kind))
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                Widen(widths, column + 1, table.Columns[column].Header);
            }

            row++;
            foreach (var values in table.Rows)
            {
                for (var column = 0; column < table.Columns.Count && column < values.Count; column++)
                {
                    var kind = table.Columns[column].Kind;
                    SetValue(sheet.Cell(row, column + 1), values[column], kind, document.Culture);
                    Widen(widths, column + 1, ReportValues.Format(values[column], kind, document.Culture));
                }

                row++;
            }
        }

        foreach (var (column, width) in widths)
            sheet.Column(column).Width = Math.Clamp(width + 2, MinColumnWidth, MaxColumnWidth);

        using var content = new MemoryStream();
        workbook.SaveAs(content);

        return content.ToArray();
    }

    private static void SetValue(IXLCell cell, object? value, ReportValueKind kind, IFormatProvider culture)
    {
        if (value is null)
            return;

        switch (kind)
        {
            case ReportValueKind.Count:
                cell.Value = ReportValues.Number(value);
                cell.Style.NumberFormat.Format = "#,##0";
                break;
            case ReportValueKind.Number:
            case ReportValueKind.Money:
                cell.Value = ReportValues.Number(value);
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case ReportValueKind.Share:
                cell.Value = ReportValues.Number(value);
                cell.Style.NumberFormat.Format = "0.00%";
                break;
            case ReportValueKind.Month when value is DateOnly month:
                cell.Value = month.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = "mmmm yyyy";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                break;
            default:
                cell.Value = ReportValues.Format(value, kind, culture);
                break;
        }
    }

    private static void Widen(Dictionary<int, int> widths, int column, string text)
    {
        var length = text.Split('\n').Max(line => line.Length);
        if (!widths.TryGetValue(column, out var width) || width < length)
            widths[column] = length;
    }

    /// <summary>Makes a worksheet name of the title: without the characters Excel forbids, and short enough.</summary>
    private static string SheetName(string title)
    {
        var parts = title.Split(_sheetNameForbidden, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var name = string.Join(' ', parts);
        if (name.Length > MaxSheetNameLength)
            name = name[..MaxSheetNameLength].TrimEnd();

        name = name.Trim('\'');

        return name.Length == 0 ? FallbackSheetName : name;
    }
}
