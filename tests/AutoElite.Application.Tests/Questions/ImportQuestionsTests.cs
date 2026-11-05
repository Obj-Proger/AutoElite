using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Questions;

public class ImportQuestionsTests
{
    private static readonly Error _categoryUnknown = ApplicationErrors.Question.ImportCategoryUnknown;

    private readonly FakeQuestionRepository _questions = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeQuestionFileReader _reader = new();
    private readonly Guid _topicId;

    public ImportQuestionsTests()
    {
        var course = TestData.NewCourse();
        _topicId = course.AddTopic("Road signs", null).Value.Id;
        _courses.Seed(course);
    }

    private async Task<Result<QuestionImportResult>> Import(
        QuestionSource source = QuestionSource.Official, FakeCurrentUser? caller = null)
    {
        caller ??= TestData.Administrator();
        await using var content = new MemoryStream([1]);

        return await new ImportQuestionsCommandHandler(
                _reader, _questions, new QuestionAccess(_questions, caller), new QuestionContent(_questions, _courses))
            .Handle(new ImportQuestionsCommand(content, "questions.xlsx", _topicId, source), TestContext.Current.CancellationToken);
    }

    private Question SeedQuestion(string externalCode, QuestionSource source = QuestionSource.Official)
    {
        var question = Question.Create(
                "Old text", null, "images/sign.png", _topicId, [LicenseCategory.B], source, externalCode,
                [new AnswerOptionDraft("Yes", true), new AnswerOptionDraft("No", false)])
            .Value;
        _questions.Seed(question);

        return question;
    }

    [Fact]
    public async Task Import_ValidRows_AddsTheQuestionsToTheTopic()
    {
        _reader.Rows = [FakeQuestionFileReader.Row(2, "1.1"), FakeQuestionFileReader.Row(3)];

        var result = await Import();

        result.Value.Should().Be(new QuestionImportResult(Created: 2, Updated: 0));
        _questions.Items.Should().HaveCount(2).And.OnlyContain(question => question.TopicId == _topicId);
    }

    [Fact]
    public async Task Import_KnownExternalCode_UpdatesThatQuestionAndKeepsItsImage()
    {
        var existing = SeedQuestion("1.1");
        _reader.Rows = [FakeQuestionFileReader.Row(2, "  1.1 ", text: "New text")];

        var result = await Import();

        result.Value.Should().Be(new QuestionImportResult(Created: 0, Updated: 1));
        existing.Text.Should().Be("New text");
        existing.ImageKey.Should().Be("images/sign.png");
        _questions.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Import_InvalidRows_ImportsNothingAndListsTheErrorsByRow()
    {
        _reader.Rows =
        [
            FakeQuestionFileReader.Row(2),
            FakeQuestionFileReader.Row(3, text: "  "),
            FakeQuestionFileReader.Row(4) with { Categories = ["Z"] }
        ];

        var result = await Import();

        result.Error.Code.Should().Be(ApplicationErrors.Question.ImportInvalid.Code);
        result.Error.FieldErrors.Should().Equal(
            new FieldError("rows[3]", DomainErrors.Question.TextEmpty.Code, DomainErrors.Question.TextEmpty.Message),
            new FieldError("rows[4]", _categoryUnknown.Code, _categoryUnknown.Message));
        _questions.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("3")]
    [InlineData("")]
    public async Task Import_CategoryThatIsANumberOrEmpty_ReportsImportCategoryUnknown(string category)
    {
        _reader.Rows = [FakeQuestionFileReader.Row(2) with { Categories = [category] }];

        var result = await Import();

        result.Error.FieldErrors!.Single().Code.Should().Be(ApplicationErrors.Question.ImportCategoryUnknown.Code);
    }

    [Fact]
    public async Task Import_SameCodeInTwoRows_ReportsBothRows()
    {
        _reader.Rows = [FakeQuestionFileReader.Row(2, "1.1"), FakeQuestionFileReader.Row(3, "1.1")];

        var result = await Import();

        result.Error.FieldErrors!.Select(error => error.Field).Should().Equal("rows[2]", "rows[3]");
        result.Error.FieldErrors!.Should()
            .OnlyContain(error => error.Code == ApplicationErrors.Question.ImportExternalCodeRepeated.Code);
    }

    [Fact]
    public async Task Import_CodeOfASchoolQuestionInAnOfficialFile_ReportsSourceMismatch()
    {
        SeedQuestion("1.1", QuestionSource.School);
        _reader.Rows = [FakeQuestionFileReader.Row(2, "1.1")];

        var result = await Import();

        result.Error.FieldErrors!.Single().Code.Should().Be(ApplicationErrors.Question.ImportSourceMismatch.Code);
    }

    [Fact]
    public async Task Import_OfficialQuestionsByATheoryInstructor_ReturnsOfficialAdministratorOnly()
    {
        _reader.Rows = [FakeQuestionFileReader.Row(2)];

        var result = await Import(caller: FakeCurrentUser.SignedIn(UserRole.TheoryInstructor));

        result.Error.Should().Be(ApplicationErrors.Question.OfficialAdministratorOnly);
    }

    [Fact]
    public async Task Import_UnreadableFile_ReturnsTheReadersError()
    {
        _reader.Failure = ApplicationErrors.Question.ImportUnreadable;

        var result = await Import();

        result.Error.Should().Be(ApplicationErrors.Question.ImportUnreadable);
    }

    [Fact]
    public async Task Import_EmptyFile_ReturnsImportEmpty()
    {
        var result = await Import();

        result.Error.Should().Be(ApplicationErrors.Question.ImportEmpty);
    }
}
