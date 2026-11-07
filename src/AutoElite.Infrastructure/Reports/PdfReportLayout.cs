using System.Globalization;
using AutoElite.Application.Analytics;
using SkiaSharp;

namespace AutoElite.Infrastructure.Reports;

/// <summary>A line of text on a page, placed by its baseline.</summary>
/// <param name="Text">The text, ready for the font.</param>
/// <param name="X">Where the text starts, centres or ends, by <paramref name="Align"/>.</param>
/// <param name="Baseline">The baseline.</param>
/// <param name="Style">The style of the font.</param>
/// <param name="Size">The size of the font in points.</param>
/// <param name="Align">How the text sits at <paramref name="X"/>.</param>
/// <param name="Color">The color.</param>
internal sealed record PdfText(
    string Text, float X, float Baseline, PdfFontStyle Style, float Size, SKTextAlign Align, SKColor Color);

/// <summary>A filled area of a page, such as the background of a table header.</summary>
/// <param name="Area">The area.</param>
/// <param name="Color">The color.</param>
internal sealed record PdfFill(SKRect Area, SKColor Color);

/// <summary>A horizontal line on a page, such as the line under a table row.</summary>
/// <param name="Left">Where the line starts.</param>
/// <param name="Right">Where the line ends.</param>
/// <param name="Y">The height of the line.</param>
/// <param name="Color">The color.</param>
internal sealed record PdfRule(float Left, float Right, float Y, SKColor Color);

/// <summary>What is drawn on a page, in drawing order: the fills, the lines, then the texts.</summary>
internal sealed class PdfPage
{
    /// <summary>Gets the filled areas.</summary>
    public List<PdfFill> Fills { get; } = [];

    /// <summary>Gets the lines.</summary>
    public List<PdfRule> Rules { get; } = [];

    /// <summary>Gets the texts.</summary>
    public List<PdfText> Texts { get; } = [];
}

/// <summary>A report laid out on pages.</summary>
/// <param name="PageSize">The size of every page in points.</param>
/// <param name="Pages">The pages.</param>
internal sealed record PdfLayout(SKSize PageSize, IReadOnlyList<PdfPage> Pages);

/// <summary>
/// Lays a report out on A4 pages (D-071): the title and the lines under it, then the tables, each with its title kept on
/// the page of its first row. Columns share the width of the page by the width of their texts, the texts wrap by words
/// (a word wider than its column breaks), numbers align to the right, and a table that goes on to a new page repeats
/// its header. Every page is numbered "page / pages" at the bottom. A report with a table of more than five columns is
/// laid out in landscape.
/// </summary>
internal sealed class PdfReportLayout
{
    /// <summary>The short side of an A4 page in points.</summary>
    public const float A4Short = 595.28f;

    /// <summary>The long side of an A4 page in points.</summary>
    public const float A4Long = 841.89f;

    /// <summary>The most columns a table may have for the report to stay in portrait.</summary>
    public const int MaxPortraitColumns = 5;

    private const float Margin = 40;
    private const float TitleSize = 16;
    private const float HeadingSize = 11;
    private const float BodySize = 9;
    private const float FooterSize = 8;
    private const float CellPaddingX = 4;
    private const float CellPaddingY = 3;
    private const float TitleGap = 6;
    private const float SectionGap = 14;

    private static readonly SKColor _ink = new(0x21, 0x21, 0x21);
    private static readonly SKColor _muted = new(0x61, 0x61, 0x61);
    private static readonly SKColor _headerFill = new(0xEE, 0xEE, 0xEE);
    private static readonly SKColor _ruleColor = new(0xBD, 0xBD, 0xBD);

    private readonly PdfFonts _fonts;
    private readonly SKSize _pageSize;
    private readonly List<PdfPage> _pages = [];
    private PdfPage _page = new();
    private float _y;

    private PdfReportLayout(PdfFonts fonts, SKSize pageSize)
    {
        _fonts = fonts;
        _pageSize = pageSize;
    }

    private float ContentWidth => _pageSize.Width - (2 * Margin);

    private float Bottom => _pageSize.Height - Margin;

    /// <summary>Lays the report out.</summary>
    /// <param name="document">The report.</param>
    /// <param name="fonts">The fonts that measure the texts.</param>
    public static PdfLayout Lay(ReportDocument document, PdfFonts fonts)
    {
        var landscape = document.Tables.Any(candidate => candidate.Columns.Count > MaxPortraitColumns);
        var layout = new PdfReportLayout(fonts, landscape ? new SKSize(A4Long, A4Short) : new SKSize(A4Short, A4Long));
        layout.NewPage();

        layout.Paragraph(document.Title, PdfFontStyle.Bold, TitleSize, _ink);
        layout._y += TitleGap;
        foreach (var detail in document.Details)
            layout.Paragraph(detail, PdfFontStyle.Regular, BodySize, _muted);

        foreach (var table in document.Tables)
        {
            layout._y += SectionGap;
            layout.Table(table, document.Culture);
        }

        layout.NumberPages();

        return new PdfLayout(layout._pageSize, layout._pages);
    }

    /// <summary>Wraps text to a width by words; a word wider than the width breaks between characters.</summary>
    /// <param name="text">The text; line breaks start new lines.</param>
    /// <param name="font">The font.</param>
    /// <param name="width">The width.</param>
    /// <returns>The lines, at least one.</returns>
    public static List<string> Wrap(string text, SKFont font, float width)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in PdfFonts.Printable(paragraph, font).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (font.MeasureText(candidate) <= width)
                {
                    line = candidate;
                    continue;
                }

                if (line.Length > 0)
                    lines.Add(line);

                line = word;
                while (font.MeasureText(line) > width)
                {
                    var fitting = FittingLength(line, font, width);
                    if (fitting >= line.Length)
                        break;

                    lines.Add(line[..fitting]);
                    line = line[fitting..];
                }
            }

            lines.Add(line);
        }

        return lines;
    }

    /// <summary>Gets the length of the longest start of a word that fits the width, at least one character.</summary>
    private static int FittingLength(string word, SKFont font, float width)
    {
        var length = StringInfo.GetNextTextElementLength(word);
        while (length < word.Length)
        {
            var next = length + StringInfo.GetNextTextElementLength(word, length);
            if (font.MeasureText(word[..next]) > width)
                break;

            length = next;
        }

        return length;
    }

    private void NewPage()
    {
        _page = new PdfPage();
        _pages.Add(_page);
        _y = Margin;
    }

    private void Paragraph(string text, PdfFontStyle style, float size, SKColor color)
    {
        var font = _fonts.Get(style, size);
        foreach (var line in Wrap(text, font, ContentWidth))
        {
            if (_y + font.Spacing > Bottom)
                NewPage();

            _page.Texts.Add(new PdfText(line, Margin, _y - font.Metrics.Ascent, style, size, SKTextAlign.Left, color));
            _y += font.Spacing;
        }
    }

    private void Table(ReportTable table, CultureInfo culture)
    {
        var widths = ColumnWidths(table, culture);
        var header = Cells(table.Columns.Select(column => column.Header).ToList(), widths, PdfFontStyle.Bold);
        var rows = table.Rows
            .Select(values => Cells(
                [.. table.Columns.Select((column, index) => ValueText(values, index, column, culture))],
                widths,
                PdfFontStyle.Regular))
            .ToList();

        var titleHeight = table.Title is null
            ? 0
            : (Wrap(table.Title, _fonts.Get(PdfFontStyle.Bold, HeadingSize), ContentWidth).Count
                * _fonts.Get(PdfFontStyle.Bold, HeadingSize).Spacing) + TitleGap;
        var firstRowHeight = rows.Count == 0 ? 0 : RowHeight(rows[0]);
        if (_y > Margin && _y + titleHeight + RowHeight(header) + firstRowHeight > Bottom)
            NewPage();

        if (table.Title is not null)
        {
            Paragraph(table.Title, PdfFontStyle.Bold, HeadingSize, _ink);
            _y += TitleGap;
        }

        Row(header, table.Columns, widths, PdfFontStyle.Bold, _headerFill);
        foreach (var row in rows)
        {
            if (_y + RowHeight(row) > Bottom)
            {
                NewPage();
                Row(header, table.Columns, widths, PdfFontStyle.Bold, _headerFill);
            }

            Row(row, table.Columns, widths, PdfFontStyle.Regular, fill: null);
        }
    }

    /// <summary>
    /// Shares the width of the page among the columns: by the widths of their texts when all fit, stretched to the
    /// width; otherwise narrow columns keep their width and the wide ones share the rest equally, wrapping their texts.
    /// </summary>
    private float[] ColumnWidths(ReportTable table, CultureInfo culture)
    {
        var bold = _fonts.Get(PdfFontStyle.Bold, BodySize);
        var regular = _fonts.Get(PdfFontStyle.Regular, BodySize);
        var desired = new float[table.Columns.Count];
        for (var index = 0; index < desired.Length; index++)
        {
            var column = table.Columns[index];
            var widest = bold.MeasureText(PdfFonts.Printable(column.Header, bold));
            foreach (var values in table.Rows)
            {
                var text = ValueText(values, index, column, culture);
                widest = Math.Max(widest, regular.MeasureText(PdfFonts.Printable(text, regular)));
            }

            desired[index] = widest + (2 * CellPaddingX);
        }

        var total = desired.Sum();
        if (total <= ContentWidth)
            return [.. desired.Select(width => width * ContentWidth / total)];

        var widths = new float[desired.Length];
        var remaining = ContentWidth;
        var left = desired.Length;
        foreach (var index in Enumerable.Range(0, desired.Length).OrderBy(position => desired[position]))
        {
            widths[index] = Math.Min(desired[index], remaining / left);
            remaining -= widths[index];
            left--;
        }

        return widths;
    }

    private static string ValueText(IReadOnlyList<object?> values, int index, ReportColumn column, CultureInfo culture) =>
        ReportValues.Format(index < values.Count ? values[index] : null, column.Kind, culture);

    private List<List<string>> Cells(IReadOnlyList<string> texts, float[] widths, PdfFontStyle style)
    {
        var font = _fonts.Get(style, BodySize);

        return [.. texts.Select((text, index) => Wrap(text, font, Math.Max(widths[index] - (2 * CellPaddingX), 1)))];
    }

    private float RowHeight(List<List<string>> cells) =>
        (cells.Max(lines => lines.Count) * _fonts.Get(PdfFontStyle.Regular, BodySize).Spacing) + (2 * CellPaddingY);

    private void Row(
        List<List<string>> cells, IReadOnlyList<ReportColumn> columns, float[] widths, PdfFontStyle style, SKColor? fill)
    {
        var height = RowHeight(cells);
        var font = _fonts.Get(style, BodySize);
        if (fill is { } color)
            _page.Fills.Add(new PdfFill(new SKRect(Margin, _y, Margin + ContentWidth, _y + height), color));

        var x = Margin;
        for (var index = 0; index < cells.Count; index++)
        {
            var numeric = ReportValues.IsNumeric(columns[index].Kind);
            var textX = numeric ? x + widths[index] - CellPaddingX : x + CellPaddingX;
            var baseline = _y + CellPaddingY - font.Metrics.Ascent;
            foreach (var line in cells[index])
            {
                _page.Texts.Add(new PdfText(
                    line, textX, baseline, style, BodySize, numeric ? SKTextAlign.Right : SKTextAlign.Left, _ink));
                baseline += font.Spacing;
            }

            x += widths[index];
        }

        _page.Rules.Add(new PdfRule(Margin, Margin + ContentWidth, _y + height, _ruleColor));
        _y += height;
    }

    private void NumberPages()
    {
        var font = _fonts.Get(PdfFontStyle.Regular, FooterSize);
        for (var index = 0; index < _pages.Count; index++)
        {
            _pages[index].Texts.Add(new PdfText(
                string.Create(CultureInfo.InvariantCulture, $"{index + 1} / {_pages.Count}"),
                _pageSize.Width / 2,
                _pageSize.Height - (Margin / 2) - (font.Metrics.Descent / 2),
                PdfFontStyle.Regular,
                FooterSize,
                SKTextAlign.Center,
                _muted));
        }
    }
}
