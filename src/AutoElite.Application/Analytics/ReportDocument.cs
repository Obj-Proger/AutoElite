using System.Globalization;

namespace AutoElite.Application.Analytics;

/// <summary>The file format of an exported report (FR-14.6).</summary>
public enum ReportFormat
{
    /// <summary>A PDF document for reading and printing.</summary>
    Pdf = 1,

    /// <summary>An Excel workbook (.xlsx) for further calculation.</summary>
    Excel = 2
}

/// <summary>How the values of a report column are written.</summary>
public enum ReportValueKind
{
    /// <summary>A <see cref="string"/>.</summary>
    Text = 1,

    /// <summary>A whole number, an <see cref="int"/>.</summary>
    Count = 2,

    /// <summary>A <see cref="decimal"/> with up to two decimal places, such as hours or an average.</summary>
    Number = 3,

    /// <summary>A <see cref="decimal"/> amount of money with two decimal places; the currency is in another column.</summary>
    Money = 4,

    /// <summary>A <see cref="decimal"/> share from 0 to 1, written as a percentage.</summary>
    Share = 5,

    /// <summary>A month, given as the <see cref="DateOnly"/> of its first day.</summary>
    Month = 6
}

/// <summary>
/// A report ready to be rendered, independent of the file format. Every text is already in the reader's language; the
/// values are kept raw, so that a workbook keeps numbers as numbers, and are formatted in <see cref="Culture"/>.
/// </summary>
/// <param name="Title">The title of the report.</param>
/// <param name="Details">The lines under the title: the period, the branch and when the report was made.</param>
/// <param name="Tables">The tables of the report, in order.</param>
/// <param name="Culture">The culture of the reader.</param>
public sealed record ReportDocument(
    string Title, IReadOnlyList<string> Details, IReadOnlyList<ReportTable> Tables, CultureInfo Culture);

/// <summary>A table of a report.</summary>
/// <param name="Title">The title of the table, if it needs one.</param>
/// <param name="Columns">The columns.</param>
/// <param name="Rows">The rows; each has one value per column, <see langword="null"/> for an empty cell.</param>
public sealed record ReportTable(string? Title, IReadOnlyList<ReportColumn> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>A column of a report table.</summary>
/// <param name="Header">The header.</param>
/// <param name="Kind">How the values are written.</param>
public sealed record ReportColumn(string Header, ReportValueKind Kind);

/// <summary>An exported report file.</summary>
/// <param name="FileName">The file name with its extension.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="Content">The content.</param>
public sealed record ReportFile(string FileName, string ContentType, byte[] Content);
