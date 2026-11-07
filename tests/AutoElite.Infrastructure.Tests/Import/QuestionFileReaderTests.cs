using System.IO.Compression;
using System.Text;
using AutoElite.Application.Common;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;
using AutoElite.Domain.Common;
using AutoElite.Infrastructure.Import;
using ClosedXML.Excel;

namespace AutoElite.Infrastructure.Tests.Import;

public sealed class QuestionFileReaderTests
{
    private const string FirstId = "ffd0c95b28a89bd4faff45a8874c4fb3";

    private const string SecondId = "40edd1d720133385413d2302583b2eee";

    private const string ThirdId = "5ccc967d70f19d41f276ce69b2ad1334";

    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Task<Result<IQuestionImportFile>> ReadAsync(byte[] content, string fileName) =>
        new QuestionFileReader().ReadAsync(new MemoryStream(content), fileName, CancellationToken);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var entry = archive.CreateEntry(name).Open();
                entry.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static string PddQuestion(string id, string set, string image, string text = "Как поступить водителю?") =>
        $$"""
        {
          "title": "Вопрос 1",
          "ticket_number": "Билет 1",
          "ticket_category": "{{set}}",
          "image": "{{image}}",
          "question": "{{text}}",
          "answers": [
            { "answer_text": "Остановиться", "is_correct": false },
            { "answer_text": "Уступить дорогу", "is_correct": true }
          ],
          "correct_answer": "Правильный ответ: 2",
          "answer_tip": "Пункт 13.1 ПДД.",
          "topic": ["Проезд перекрёстков"],
          "id": "{{id}}"
        }
        """;

    private static byte[] PddTicket(params string[] questions) => Utf8($"[{string.Join(',', questions)}]");

    private static void ShouldFail(Result<IQuestionImportFile> result, Error error, string field, string reason)
    {
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(error.Code);
        var detail = result.Error.FieldErrors.Should().ContainSingle().Which;
        detail.Field.Should().Be(field);
        detail.Message.Should().Contain(reason);
    }

    [Fact]
    public async Task Read_PddRussiaJson_ReadsTheQuestionsWithTheCategoriesOfTheirSet()
    {
        var content = PddTicket(
            PddQuestion(FirstId, "A,B", "./images/no_image.jpg"),
            PddQuestion(SecondId, "C,D", "./images/C_D/1871b903.jpg"));

        var result = await ReadAsync(content, "Билет 1.json");

        await using var file = result.Value;
        var rows = file.Rows;
        rows.Should().HaveCount(2);
        rows[0].RowNumber.Should().Be(1);
        rows[0].Text.Should().Be("Как поступить водителю?");
        rows[0].Explanation.Should().Be("Пункт 13.1 ПДД.");
        rows[0].Categories.Should().Equal("A", "A1", "B", "B1", "M");
        rows[0].ExternalCode.Should().Be($"pdd_russia:AB:{FirstId}");
        rows[0].Options.Should().Equal(new AnswerOptionInput("Остановиться", false), new AnswerOptionInput("Уступить дорогу", true));
        rows[0].ImageName.Should().BeNull();
        rows[1].RowNumber.Should().Be(2);
        rows[1].Categories.Should().Equal("C", "C1", "D", "D1");
        rows[1].ExternalCode.Should().Be($"pdd_russia:CD:{SecondId}");
        rows[1].ImageName.Should().Be("./images/C_D/1871b903.jpg");
        file.GetImageSize(rows[1].ImageName!).Should().BeNull();
    }

    [Fact]
    public async Task Read_PddRussiaArchive_FindsThePicturesAndReadsAQuestionOfSeveralTicketsOnce()
    {
        var content = Zip(
            ("pdd_russia-master/questions/A_B/tickets/Билет 1.json", PddTicket(
                PddQuestion(FirstId, "A,B", "./images/A_B/a.jpg"), PddQuestion(SecondId, "A,B", "./images/no_image.jpg"))),
            ("pdd_russia-master/questions/A_B/tickets/Билет 2.json", PddTicket(
                PddQuestion(FirstId, "A,B", "./images/A_B/a.jpg"), PddQuestion(ThirdId, "A,B", "./images/no_image.jpg"))),
            ("pdd_russia-master/images/A_B/a.jpg", _jpeg),
            ("pdd_russia-master/README.md", Utf8("# pdd_russia")));

        var result = await ReadAsync(content, "pdd.zip");

        await using var file = result.Value;
        file.Rows.Select(row => row.ExternalCode).Should().Equal(
            $"pdd_russia:AB:{FirstId}", $"pdd_russia:AB:{SecondId}", $"pdd_russia:AB:{ThirdId}");
        file.Rows.Select(row => row.RowNumber).Should().Equal(1, 2, 3);
        var imageName = file.Rows[0].ImageName!;
        file.GetImageSize(imageName).Should().Be(_jpeg.Length);
        await using var image = await file.OpenImageAsync(imageName, CancellationToken);
        using var copy = new MemoryStream();
        await image.CopyToAsync(copy, CancellationToken);
        copy.ToArray().Should().Equal(_jpeg);
    }

    [Fact]
    public async Task Read_Archive_FindsAnImageByItsPathOnly_WhenTheNameIsUnambiguousAndSafe()
    {
        var content = Zip(
            ("questions.json", PddTicket(PddQuestion(FirstId, "A,B", "./images/no_image.jpg"))),
            ("a/images/x.png", _jpeg),
            ("b/images/x.png", _jpeg),
            ("a/images/y.png", _jpeg));

        var result = await ReadAsync(content, "questions.zip");

        await using var file = result.Value;
        file.GetImageSize("a/images/x.png").Should().Be(_jpeg.Length);
        file.GetImageSize(@"A\Images\X.png").Should().Be(_jpeg.Length);
        file.GetImageSize("./images/y.png").Should().Be(_jpeg.Length);
        file.GetImageSize("images/x.png").Should().BeNull();
        file.GetImageSize("../a/images/y.png").Should().BeNull();
        file.GetImageSize("images/z.png").Should().BeNull();
    }

    [Fact]
    public async Task Read_AutoEliteJson_ReadsEveryPartOfAQuestion()
    {
        var content = Utf8("""
            {
              "format": "autoelite.questions",
              "version": 1,
              "exportedAtUtc": "2026-11-08T10:00:00Z",
              "questions": [
                {
                  "code": "S-12",
                  "text": "Какой знак запрещает остановку?",
                  "explanation": "Знак 3.27.",
                  "categories": ["B", "C1"],
                  "image": "images/s-12.png",
                  "options": [
                    { "text": "3.27", "isCorrect": true },
                    { "text": "3.28" }
                  ]
                }
              ]
            }
            """);

        var result = await ReadAsync(content, "school.json");

        await using var file = result.Value;
        var row = file.Rows.Should().ContainSingle().Which;
        row.Should().BeEquivalentTo(new
        {
            RowNumber = 1,
            Text = "Какой знак запрещает остановку?",
            Explanation = "Знак 3.27.",
            ExternalCode = "S-12",
            ImageName = "images/s-12.png"
        });
        row.Categories.Should().Equal("B", "C1");
        row.Options.Should().Equal(new AnswerOptionInput("3.27", true), new AnswerOptionInput("3.28", false));
    }

    [Theory]
    [InlineData("""{ "format": "autoelite.questions", "version": 2, "questions": [] }""")]
    [InlineData("""{ "Предупреждающие знаки": {} }""")]
    public async Task Read_JsonInNoKnownFormat_IsNotSupported(string json)
    {
        var result = await ReadAsync(Utf8(json), "questions.json");

        ShouldFail(result, ApplicationErrors.Question.ImportFormatNotSupported, QuestionFileReader.FileField, "pdd_russia");
    }

    [Theory]
    [InlineData("""[{ "question": "Q", "answers": "Да" }]""", "[0].answers must be a list")]
    [InlineData("""[{ "question": "Q", "answers": [{ "answer_text": 2 }] }]""", "[0].answers[0].answer_text must be text")]
    [InlineData("""{ "format": "autoelite.questions", "version": "1" }""", "version must be a whole number")]
    [InlineData("""{ "format": "autoelite.questions", "version": 1 }""", "questions must be a list")]
    [InlineData("""[{ "question": "Q" """, "LineNumber")]
    public async Task Read_JsonThatCannotBeRead_SaysWhy(string json, string reason)
    {
        var result = await ReadAsync(Utf8(json), "questions.json");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, reason);
    }

    [Fact]
    public async Task Read_CsvFromARussianExcel_ReadsWindows1251WithSemicolonsAndRussianColumns()
    {
        var text = "Код;Вопрос;Вариант 1;Вариант 2;Вариант 3;Правильный ответ;Категории;Пояснение;Тема\r\n"
            + "Q-1;\"Что означает \"\"знак\"\"?\";Да;Нет;;2;B, C;Пункт 1.;Общие\r\n";
        var content = CodePagesEncodingProvider.Instance.GetEncoding(1251)!.GetBytes(text);

        var result = await ReadAsync(content, "вопросы.csv");

        await using var file = result.Value;
        var row = file.Rows.Should().ContainSingle().Which;
        row.RowNumber.Should().Be(2);
        row.ExternalCode.Should().Be("Q-1");
        row.Text.Should().Be("Что означает \"знак\"?");
        row.Options.Should().Equal(new AnswerOptionInput("Да", false), new AnswerOptionInput("Нет", true));
        row.Categories.Should().Equal("B", "C");
        row.Explanation.Should().Be("Пункт 1.");
        row.ImageName.Should().BeNull();
    }

    [Fact]
    public async Task Read_CsvInUtf8_ReadsLineBreaksInQuotesAndSkipsEmptyRows()
    {
        var text = "question,option1,option2,correct,image\r\n"
            + "\"Line one\nline two\",A,\"B, or C\",1, img/1.png \r\n"
            + "\r\n"
            + ",,,,\n"
            + "Second,C,D,2,";
        var content = Encoding.UTF8.GetPreamble().Concat(Utf8(text)).ToArray();

        var result = await ReadAsync(content, "questions.csv");

        await using var file = result.Value;
        file.Rows.Select(row => row.RowNumber).Should().Equal(2, 5);
        file.Rows[0].Text.Should().Be("Line one\nline two");
        file.Rows[0].Options.Should().Equal(new AnswerOptionInput("A", true), new AnswerOptionInput("B, or C", false));
        file.Rows[0].ImageName.Should().Be("img/1.png");
        file.Rows[1].Options.Should().Equal(new AnswerOptionInput("C", false), new AnswerOptionInput("D", true));
        file.Rows[1].ImageName.Should().BeNull();
    }

    [Theory]
    [InlineData("question,correct\r\nQ,1", "must name the columns")]
    [InlineData("question,Question,correct,option1\r\nQ,Q,1,A", "\"Question\" is named twice")]
    [InlineData("question,correct,option1\r\n\"Q,1,A", "not closed")]
    public async Task Read_CsvThatCannotBeRead_SaysWhy(string text, string reason)
    {
        var result = await ReadAsync(Utf8(text), "questions.csv");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, reason);
    }

    [Fact]
    public async Task Read_Xlsx_ReadsTheFirstWorksheetWithNumbersAsTheyAreWritten()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Questions");
        sheet.Cell(1, 1).Value = "Question";
        sheet.Cell(1, 2).Value = "Option 1";
        sheet.Cell(1, 3).Value = "Option 2";
        sheet.Cell(1, 4).Value = "Correct";
        sheet.Cell(1, 5).Value = "Categories";
        sheet.Cell(2, 1).Value = "First";
        sheet.Cell(2, 2).Value = "A";
        sheet.Cell(2, 3).Value = 12.5;
        sheet.Cell(2, 4).Value = 2;
        sheet.Cell(2, 5).Value = "B";
        sheet.Cell(4, 1).Value = "Second";
        sheet.Cell(4, 2).Value = "C";
        sheet.Cell(4, 3).Value = "D";
        sheet.Cell(4, 4).Value = 1;
        workbook.AddWorksheet("Notes").Cell(1, 1).Value = "Ignored";
        using var saved = new MemoryStream();
        workbook.SaveAs(saved);

        var result = await ReadAsync(saved.ToArray(), "questions.xlsx");

        await using var file = result.Value;
        file.Rows.Select(row => row.RowNumber).Should().Equal(2, 4);
        file.Rows[0].Options.Should().Equal(new AnswerOptionInput("A", false), new AnswerOptionInput("12.5", true));
        file.Rows[0].Categories.Should().Equal("B");
        file.Rows[1].Options.Should().Equal(new AnswerOptionInput("C", true), new AnswerOptionInput("D", false));
        file.Rows[1].Categories.Should().BeEmpty();
    }

    [Theory]
    [InlineData("questions.xlsx", "not a valid Excel workbook")]
    [InlineData("questions.zip", "not a valid ZIP archive")]
    public async Task Read_FileThatIsNotAZipPackage_IsUnreadable(string fileName, string reason)
    {
        var result = await ReadAsync(Utf8("question,correct,option1"), fileName);

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, reason);
    }

    [Theory]
    [InlineData("questions.txt")]
    [InlineData("questions")]
    [InlineData("questions.xls")]
    public async Task Read_FileOfAnotherExtension_IsNotSupported(string fileName)
    {
        var result = await ReadAsync(Utf8("question,correct,option1"), fileName);

        result.Error.Should().Be(ApplicationErrors.Question.ImportFormatNotSupported);
    }

    [Fact]
    public async Task Read_ArchiveWithANameLeadingOutOfItsFolder_IsUnreadable()
    {
        var content = Zip(("questions.json", PddTicket()), ("images/../../evil.png", _jpeg));

        var result = await ReadAsync(content, "questions.zip");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, "unsafe name");
    }

    [Fact]
    public async Task Read_ArchiveWithANameTwice_IsUnreadable()
    {
        var content = Zip(("questions.json", PddTicket()), ("images/a.png", _jpeg), ("Images/A.png", _jpeg));

        var result = await ReadAsync(content, "questions.zip");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, "more than once");
    }

    [Fact]
    public async Task Read_ArchivePackedTooTightly_IsUnreadable()
    {
        var content = Zip(("questions.json", PddTicket()), ("images/bomb.png", new byte[2 * 1024 * 1024]));

        var result = await ReadAsync(content, "questions.zip");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, "packed too tightly");
    }

    [Fact]
    public async Task Read_ArchiveWithoutQuestions_IsUnreadable()
    {
        var content = Zip(("images/a.png", _jpeg), ("__MACOSX/._questions.json", _jpeg));

        var result = await ReadAsync(content, "questions.zip");

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, "no file of questions");
    }

    [Fact]
    public async Task Read_ArchiveWithAFileThatCannotBeRead_NamesTheFile()
    {
        var content = Zip(
            ("questions/tickets/1.json", PddTicket(PddQuestion(FirstId, "A,B", "./images/no_image.jpg"))),
            ("signs/signs.json", Utf8("""{ "Предупреждающие знаки": {} }""")),
            ("table.csv", Utf8("question\r\nQ")));

        var result = await ReadAsync(content, "questions.zip");

        ShouldFail(
            result, ApplicationErrors.Question.ImportFormatNotSupported, $"{QuestionFileReader.FileField}[signs/signs.json]", "pdd_russia");
    }

    [Fact]
    public async Task Read_UploadLargerThanTheLimit_StopsReadingAtTheLimit()
    {
        using var content = new EndlessStream();

        var result = await new QuestionFileReader().ReadAsync(content, "questions.json", CancellationToken);

        ShouldFail(result, ApplicationErrors.Question.ImportUnreadable, QuestionFileReader.FileField, "larger than 32 MB");
        content.BytesRead.Should().BeLessThan(ImportLimits.MaxQuestionFileBytes + (1024 * 1024));
    }

    [Fact]
    public void BoundedStream_ContentLongerThanDeclared_Throws_AndContentOfTheDeclaredLengthIsRead()
    {
        using var exact = new BoundedReadStream(new MemoryStream(_jpeg), _jpeg.Length);
        using var longer = new BoundedReadStream(new MemoryStream(_jpeg), _jpeg.Length - 1);
        using var copy = new MemoryStream();

        exact.CopyTo(copy);
        var readLonger = () => longer.CopyTo(Stream.Null);

        copy.ToArray().Should().Equal(_jpeg);
        readLonger.Should().Throw<InvalidDataException>();
    }

    /// <summary>A stream of zeros that never ends, counting what is read.</summary>
    private sealed class EndlessStream : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => BytesRead;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            BytesRead += count;

            return count;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
