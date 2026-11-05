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
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeFileStorage _storage = new();
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
                _reader, _questions, new QuestionAccess(_questions, caller), new QuestionContent(_questions, _courses), _documents,
                _storage, caller, TestData.Clock())
            .Handle(new ImportQuestionsCommand(content, "questions.xlsx", _topicId, source), TestContext.Current.CancellationToken);
    }

    private Question SeedQuestion(string externalCode, QuestionSource source = QuestionSource.Official)
    {
        var question = Question.Create(
                "Old text", null, _topicId, [LicenseCategory.B], source, externalCode,
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

        result.Value.Should().Be(new QuestionImportResult(Created: 2, Updated: 0, Images: 0));
        _questions.Items.Should().HaveCount(2).And.OnlyContain(question => question.TopicId == _topicId);
        _reader.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task Import_RowWithImage_StoresItAsADocumentOfTheQuestion()
    {
        _reader.AddImage("A_B/1.png", [1, 2, 3]);
        _reader.Rows = [FakeQuestionFileReader.Row(2, "1.1", imageName: "A_B/1.png")];

        var result = await Import();

        result.Value.Should().Be(new QuestionImportResult(Created: 1, Updated: 0, Images: 1));
        var question = _questions.Items.Single();
        var image = _documents.Items.Single();
        image.Owner.Should().Be(EntityReference.Create(EntityKind.Question, question.Id).Value);
        image.FileName.Should().Be("1.png");
        image.ContentType.Should().Be("image/png");
        question.ImageDocumentId.Should().Be(image.Id);
        _storage.Files[image.StorageKey].Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Import_KnownExternalCodeWithImage_ReplacesTheIllustration()
    {
        var existing = SeedQuestion("1.1");
        existing.SetImage(Guid.CreateVersion7()).IsSuccess.Should().BeTrue();
        _reader.AddImage("1.jpg", [1, 2]);
        _reader.Rows = [FakeQuestionFileReader.Row(2, "1.1", imageName: "1.jpg")];

        var result = await Import();

        result.Value.Should().Be(new QuestionImportResult(Created: 0, Updated: 1, Images: 1));
        existing.ImageDocumentId.Should().Be(_documents.Items.Single().Id);
    }

    [Fact]
    public async Task Import_ImageNotInTheFile_ReportsTheRowAndStoresNothing()
    {
        _reader.AddImage("1.png", [1]);
        _reader.Rows =
        [
            FakeQuestionFileReader.Row(2, imageName: "1.png"),
            FakeQuestionFileReader.Row(3, imageName: "2.png")
        ];

        var result = await Import();

        var missing = ApplicationErrors.Question.ImportImageMissing;
        result.Error.FieldErrors.Should().Equal(new FieldError("rows[3]", missing.Code, missing.Message));
        _storage.Files.Should().BeEmpty();
        _questions.Items.Should().BeEmpty();
        _documents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_ImageThatIsNotAPicture_ReportsImageRequired()
    {
        _reader.AddImage("sign.pdf", [1]);
        _reader.Rows = [FakeQuestionFileReader.Row(2, imageName: "sign.pdf")];

        var result = await Import();

        result.Error.FieldErrors!.Single().Code.Should().Be(ApplicationErrors.Document.ImageRequired.Code);
    }

    [Fact]
    public async Task Import_ImagesTheStorageRejects_ReportsEveryRowAndAddsNothing()
    {
        _storage.RejectSignature = true;
        _reader.AddImage("1.png", [1]);
        _reader.AddImage("2.png", [2]);
        _reader.Rows =
        [
            FakeQuestionFileReader.Row(2, imageName: "1.png"),
            FakeQuestionFileReader.Row(3),
            FakeQuestionFileReader.Row(4, imageName: "2.png")
        ];

        var result = await Import();

        result.Error.Code.Should().Be(ApplicationErrors.Question.ImportInvalid.Code);
        result.Error.FieldErrors!.Select(error => error.Field).Should().Equal("rows[2]", "rows[4]");
        result.Error.FieldErrors!.Should().OnlyContain(error => error.Code == ApplicationErrors.Document.ContentMismatch.Code);
        _questions.Items.Should().BeEmpty();
        _documents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_KnownExternalCode_UpdatesThatQuestionAndKeepsItsImage()
    {
        var existing = SeedQuestion("1.1");
        var imageId = Guid.CreateVersion7();
        existing.SetImage(imageId).IsSuccess.Should().BeTrue();
        _reader.Rows = [FakeQuestionFileReader.Row(2, "  1.1 ", text: "New text")];

        var result = await Import();

        result.Value.Should().Be(new QuestionImportResult(Created: 0, Updated: 1, Images: 0));
        existing.Text.Should().Be("New text");
        existing.ImageDocumentId.Should().Be(imageId);
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
