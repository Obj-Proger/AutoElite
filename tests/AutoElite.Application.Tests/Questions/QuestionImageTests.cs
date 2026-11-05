using AutoElite.Application.Documents.AttachDocument;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.RemoveQuestionImage;
using AutoElite.Application.Questions.SetQuestionImage;
using AutoElite.Application.Tests.Documents;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Questions;

public class QuestionImageTests
{
    private readonly DocumentFixture _fixture = new();
    private readonly FakeCurrentUser _instructor = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);

    private Question SeedQuestion(QuestionSource source = QuestionSource.School)
    {
        var question = Question.Create(
                "What does the sign mean?", null, Guid.CreateVersion7(), [LicenseCategory.B], source, null,
                [new AnswerOptionDraft("Stop", true), new AnswerOptionDraft("Go", false)])
            .Value;
        _fixture.Questions.Seed(question);

        return question;
    }

    private Document SeedImage(Question question, string fileName = "sign.png", string contentType = "image/png")
    {
        var document = Document.Attach(
                EntityReference.Create(EntityKind.Question, question.Id).Value, fileName, contentType, 3,
                $"documents/{Guid.CreateVersion7():N}", null, Guid.CreateVersion7(), TestData.Now)
            .Value;
        _fixture.Documents.Seed(document);

        return document;
    }

    private Task<Result> SetImage(Question question, Document document, FakeCurrentUser caller) =>
        new SetQuestionImageCommandHandler(new QuestionAccess(_fixture.Questions, caller), _fixture.Documents)
            .Handle(new SetQuestionImageCommand(question.Id, document.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task SetImage_ImageOfTheQuestion_SetsIt()
    {
        var question = SeedQuestion();
        var image = SeedImage(question);

        var result = await SetImage(question, image, _instructor);

        result.IsSuccess.Should().BeTrue();
        question.ImageDocumentId.Should().Be(image.Id);
    }

    [Fact]
    public async Task SetImage_ImageOfAnotherQuestion_ReturnsDocumentNotFound()
    {
        var question = SeedQuestion();
        var image = SeedImage(SeedQuestion());

        var result = await SetImage(question, image, _instructor);

        result.Error.Should().Be(ApplicationErrors.NotFound<Document>());
    }

    [Fact]
    public async Task SetImage_Pdf_ReturnsImageRequired()
    {
        var question = SeedQuestion();
        var scan = SeedImage(question, "rules.pdf", "application/pdf");

        var result = await SetImage(question, scan, _instructor);

        result.Error.Should().Be(ApplicationErrors.Document.ImageRequired);
    }

    [Fact]
    public async Task SetImage_OfficialQuestionByATheoryInstructor_ReturnsOfficialAdministratorOnly()
    {
        var question = SeedQuestion(QuestionSource.Official);

        var result = await SetImage(question, SeedImage(question), _instructor);

        result.Error.Should().Be(ApplicationErrors.Question.OfficialAdministratorOnly);
    }

    [Fact]
    public async Task RemoveImage_QuestionWithImage_ClearsIt()
    {
        var question = SeedQuestion();
        question.SetImage(SeedImage(question).Id).IsSuccess.Should().BeTrue();

        var result = await new RemoveQuestionImageCommandHandler(new QuestionAccess(_fixture.Questions, _instructor))
            .Handle(new RemoveQuestionImageCommand(question.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        question.ImageDocumentId.Should().BeNull();
    }

    [Fact]
    public async Task ImageDocumentDeleted_ClearsTheIllustration()
    {
        var question = SeedQuestion();
        var image = SeedImage(question);
        question.SetImage(image.Id).IsSuccess.Should().BeTrue();
        image.Delete(TestData.Now).IsSuccess.Should().BeTrue();

        await new RemoveImageWhenDocumentDeletedHandler(_fixture.Questions).Handle(
            image.DomainEvents.OfType<DocumentDeletedDomainEvent>().Single(), TestContext.Current.CancellationToken);

        question.ImageDocumentId.Should().BeNull();
    }

    [Theory]
    [InlineData(QuestionSource.School, true)]
    [InlineData(QuestionSource.Official, false)]
    public async Task Attach_TheoryInstructor_AttachesOnlyToSchoolQuestions(QuestionSource source, bool allowed)
    {
        var question = SeedQuestion(source);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await new AttachDocumentCommandHandler(
                _fixture.Documents, _fixture.CreateOwners(_instructor), _fixture.Storage, _instructor, TestData.Clock())
            .Handle(
                new AttachDocumentCommand(EntityKind.Question, question.Id, "sign.png", "image/png", 3, null, content),
                TestContext.Current.CancellationToken);

        result.IsSuccess.Should().Be(allowed);
        if (!allowed)
            result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
    }
}
