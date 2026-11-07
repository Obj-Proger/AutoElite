using System.Globalization;
using System.Text;
using AutoElite.Application.Analytics;
using AutoElite.Infrastructure.Reports;
using ClosedXML.Excel;

namespace AutoElite.Infrastructure.Tests.Reports;

public sealed class ReportRendererTests
{
    private static readonly CultureInfo _russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly CultureInfo _english = CultureInfo.GetCultureInfo("en-US");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ReportDocument Revenue(int months, CultureInfo culture) =>
        new(
            "Выручка / по месяцам",
            ["Период: 01.01.2026 — 31.12.2026", "Вся школа"],
            [
                new ReportTable(
                    "Сводка",
                    [new ReportColumn("Лиды", ReportValueKind.Count), new ReportColumn("Конверсия", ReportValueKind.Share)],
                    [[1234, 0.1234m], [0, null]]),
                new ReportTable(
                    null,
                    [
                        new ReportColumn("Месяц", ReportValueKind.Month),
                        new ReportColumn("Валюта", ReportValueKind.Text),
                        new ReportColumn("Получено", ReportValueKind.Money),
                        new ReportColumn("Часы", ReportValueKind.Number)
                    ],
                    [
                        .. Enumerable.Range(0, months).Select(index =>
                            new object?[] { new DateOnly(2026, 1, 1).AddMonths(index), "RUB", 125000.5m + index, 1.5m })
                    ])
            ],
            culture);

    private static ReportDocument Wide(int rows, string cell = "Иванов Иван") =>
        new(
            "Нагрузка инструкторов",
            [],
            [
                new ReportTable(
                    null,
                    [
                        .. Enumerable.Range(1, 8).Select(index => new ReportColumn(
                            string.Create(CultureInfo.InvariantCulture, $"Колонка {index}"), ReportValueKind.Text))
                    ],
                    [.. Enumerable.Range(0, rows).Select(_ => Enumerable.Repeat<object?>(cell, 8).ToArray())])
            ],
            _russian);

    [Fact]
    public async Task Render_Excel_KeepsNumbersAndMonthsAsValuesWithTheirFormats()
    {
        var content = await new ReportRenderer().RenderAsync(Revenue(2, _russian), ReportFormat.Excel, CancellationToken);

        using var workbook = new XLWorkbook(new MemoryStream(content));
        var sheet = workbook.Worksheet(1);
        sheet.Name.Should().Be("Выручка по месяцам");
        sheet.Cell(1, 1).Value.GetText().Should().Be("Выручка / по месяцам");
        sheet.Cell(2, 1).Value.GetText().Should().Be("Период: 01.01.2026 — 31.12.2026");
        sheet.Cell(3, 1).Value.GetText().Should().Be("Вся школа");
        sheet.Cell(5, 1).Value.GetText().Should().Be("Сводка");
        sheet.Cell(6, 1).Value.GetText().Should().Be("Лиды");
        sheet.Cell(6, 1).Style.Font.Bold.Should().BeTrue();
        sheet.Cell(7, 1).Value.GetNumber().Should().Be(1234);
        sheet.Cell(7, 1).Style.NumberFormat.Format.Should().Be("#,##0");
        sheet.Cell(7, 2).Value.GetNumber().Should().BeApproximately(0.1234, 1e-9);
        sheet.Cell(7, 2).Style.NumberFormat.Format.Should().Be("0.00%");
        sheet.Cell(8, 2).Value.IsBlank.Should().BeTrue();
        sheet.Cell(10, 1).Value.GetText().Should().Be("Месяц");
        sheet.Cell(11, 1).Value.GetDateTime().Should().Be(new DateTime(2026, 1, 1));
        sheet.Cell(11, 1).Style.NumberFormat.Format.Should().Be("mmmm yyyy");
        sheet.Cell(11, 2).Value.GetText().Should().Be("RUB");
        sheet.Cell(12, 3).Value.GetNumber().Should().Be(125001.5);
        sheet.Cell(12, 4).Style.NumberFormat.Format.Should().Be("#,##0.00");
        sheet.Column(1).Width.Should().BeGreaterThan(8);
    }

    [Fact]
    public async Task Render_Pdf_WritesAPdfDocument()
    {
        var content = await new ReportRenderer().RenderAsync(Revenue(3, _russian), ReportFormat.Pdf, CancellationToken);

        Encoding.ASCII.GetString(content, 0, 5).Should().Be("%PDF-");
        Encoding.ASCII.GetString(content).Should().Contain("%%EOF");
    }

    [Fact]
    public async Task Render_FormatNotDefined_Throws()
    {
        var render = () => new ReportRenderer().RenderAsync(Revenue(1, _russian), (ReportFormat)99, CancellationToken);

        await render.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Layout_LongTable_GoesOnToNewPagesRepeatingTheHeaderAndNumbersThem()
    {
        using var fonts = new PdfFonts();

        var layout = PdfReportLayout.Lay(Revenue(120, _russian), fonts);

        layout.PageSize.Width.Should().Be(PdfReportLayout.A4Short);
        layout.Pages.Should().HaveCountGreaterThan(2);
        layout.Pages.Skip(1).Should().OnlyContain(page => page.Texts.Any(text => text.Text == "Месяц"));
        layout.Pages.Select(page => page.Texts[^1].Text).Should().Equal(
            Enumerable.Range(1, layout.Pages.Count)
                .Select(number => string.Create(CultureInfo.InvariantCulture, $"{number} / {layout.Pages.Count}")));
        layout.Pages.SelectMany(page => page.Texts)
            .Should().OnlyContain(text => text.Baseline > 0 && text.Baseline < layout.PageSize.Height);
    }

    [Fact]
    public void Layout_TableOfManyColumns_IsInLandscape_AndAWordWiderThanItsColumnBreaks()
    {
        using var fonts = new PdfFonts();
        var word = new string('Ж', 80);

        var layout = PdfReportLayout.Lay(Wide(1, word), fonts);

        layout.PageSize.Width.Should().Be(PdfReportLayout.A4Long);
        var font = fonts.Get(PdfFontStyle.Regular, 9);
        var pieces = layout.Pages[0].Texts.Where(text => text.Text.All(character => character == 'Ж')).ToList();
        pieces.Should().HaveCountGreaterThan(8);
        string.Concat(pieces.Take(pieces.Count / 8).Select(text => text.Text)).Should().Be(word);
        pieces.Should().OnlyContain(text => font.MeasureText(text.Text) < (PdfReportLayout.A4Long - 80) / 8);
    }

    [Fact]
    public void Wrap_BreaksBetweenWords_AndKeepsLineBreaks()
    {
        using var fonts = new PdfFonts();
        var font = fonts.Get(PdfFontStyle.Regular, 9);
        var width = font.MeasureText("Первое второе");

        var lines = PdfReportLayout.Wrap("Первое второе третье\nчетвёртое", font, width);

        lines.Should().Equal("Первое второе", "третье", "четвёртое");
    }

    [Fact]
    public void Printable_SpaceTheFontLacks_BecomesANoBreakSpace()
    {
        using var fonts = new PdfFonts();

        PdfFonts.Printable("1\u202F234\u00A0₽", fonts.Get(PdfFontStyle.Regular, 9)).Should().Be("1\u00A0234\u00A0₽");
    }

    [Theory]
    [InlineData(ReportValueKind.Count, 1234, "1,234")]
    [InlineData(ReportValueKind.Number, 1.5, "1.5")]
    [InlineData(ReportValueKind.Money, 1234.5, "1,234.50")]
    [InlineData(ReportValueKind.Share, 0.125, "12.5%")]
    [InlineData(ReportValueKind.Text, "RUB", "RUB")]
    [InlineData(ReportValueKind.Money, null, "")]
    public void Format_WritesValuesInTheCulture(ReportValueKind kind, object? value, string expected)
    {
        var number = value is double real ? (object)(decimal)real : value;

        ReportValues.Format(number, kind, _english).Should().Be(expected);
    }

    [Fact]
    public void Format_Month_IsWrittenByName()
    {
        ReportValues.Format(new DateOnly(2026, 11, 1), ReportValueKind.Month, _english).Should().Be("November 2026");
    }
}
