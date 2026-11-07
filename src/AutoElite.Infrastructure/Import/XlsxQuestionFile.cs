using System.Globalization;
using ClosedXML.Excel;

namespace AutoElite.Infrastructure.Import;

/// <summary>
/// Reads a table of questions (<see cref="QuestionTable"/>) from the first worksheet of an Excel workbook with ClosedXML.
/// A workbook is a ZIP package, so its limits are checked before ClosedXML unpacks it into memory. Cells are read as
/// they were last calculated, without evaluating formulas, and numbers in the invariant culture, so that the number of
/// the correct option reads as <c>2</c>.
/// </summary>
internal static class XlsxQuestionFile
{
    private const string NotAWorkbook = "The file is not a valid Excel workbook.";

    /// <summary>Reads the questions of the workbook.</summary>
    /// <param name="content">The content.</param>
    /// <exception cref="InvalidDataException">The content is not a workbook, breaks a limit, or lacks a column.</exception>
    public static ParsedQuestionFile Parse(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using (var package = ZipPackages.Open(stream, leaveOpen: true, NotAWorkbook))
        {
            ArchiveLimits.Check(package, ImportLimits.MaxWorkbookContentBytes);
        }

        stream.Position = 0;
        using var workbook = Open(stream);
        var sheet = workbook.Worksheet(1);
        var lastColumn = Math.Min(sheet.LastColumnUsed()?.ColumnNumber() ?? 0, ImportLimits.MaxColumns);
        var table = new List<TableRow> { new(1, Cells(sheet.Row(1), lastColumn)) };
        foreach (var row in sheet.RowsUsed())
        {
            if (row.RowNumber() > 1)
                table.Add(new TableRow(row.RowNumber(), Cells(row, lastColumn)));
        }

        return new ParsedQuestionFile(QuestionTable.Read(table), RepeatsQuestions: false);
    }

    private static XLWorkbook Open(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // ClosedXML reports a damaged package or workbook with exceptions of many types.
            throw new InvalidDataException(NotAWorkbook, exception);
        }
    }

    private static string[] Cells(IXLRow row, int lastColumn) =>
        [.. Enumerable.Range(1, lastColumn).Select(column => row.Cell(column).CachedValue.ToString(CultureInfo.InvariantCulture))];
}
