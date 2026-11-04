using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.CreateQuestion;
using AutoElite.Application.Questions.DeactivateQuestion;
using AutoElite.Application.Questions.GetQuestion;
using AutoElite.Application.Questions.GetQuestions;
using AutoElite.Application.Questions.ReactivateQuestion;
using AutoElite.Application.Questions.UpdateQuestion;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Questions;

public class QuestionHandlerTests
{
    private static readonly AnswerOptionInput[] _options =
        [new("Stop", false), new("Give way", true), new("Speed up", false)];

    private readonly FakeQuestionRepository _questions = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeQuestionQueries _queries = new();
    private readonly FakeCurrentUser _instructor = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);
    private readonly Guid _topicId;

    public QuestionHandlerTests()
    {
        var course = TestData.NewCourse();
        _topicId = course.AddTopic("Road signs", null).Value.Id;
        _courses.Seed(course);
    }

    private QuestionAccess Access(FakeCurrentUser caller) => new(_questions, caller);

    private Task<Result<Guid>> Create(
        FakeCurrentUser caller, QuestionSource source = QuestionSource.School, string? externalCode = null, Guid? topicId = null) =>
        new CreateQuestionCommandHandler(_questions, Access(caller), new QuestionContent(_questions, _courses)).Handle(
            new CreateQuestionCommand(
                "  What does the sign mean?  ", "Yield to the main road.", topicId ?? _topicId, [LicenseCategory.B, LicenseCategory.A],
                source, externalCode, _options),
            TestContext.Current.CancellationToken);

    private Task<Result> Update(Question question, FakeCurrentUser caller, string? externalCode = null) =>
        new UpdateQuestionCommandHandler(Access(caller), new QuestionContent(_questions, _courses)).Handle(
            new UpdateQuestionCommand(
                question.Id, "Who goes first?", null, _topicId, [LicenseCategory.C], externalCode,
                [new AnswerOptionInput("The tram", true), new AnswerOptionInput("The car", false)]),
            TestContext.Current.CancellationToken);

    private Question Seed(QuestionSource source = QuestionSource.School, string? externalCode = null)
    {
        var question = Question.Create(
                "Old text", null, "images/q1.png", _topicId, [LicenseCategory.B], source, externalCode,
                [new AnswerOptionDraft("Yes", true), new AnswerOptionDraft("No", false)])
            .Value;
        _questions.Seed(question);

        return question;
    }

    [Fact]
    public async Task Create_SchoolQuestionByAnInstructor_AddsItWithTheOptionsInOrder()
    {
        var result = await Create(_instructor);

        var question = _questions.Items.Should().ContainSingle().Which;
        question.Id.Should().Be(result.Value);
        question.Text.Should().Be("What does the sign mean?");
        question.Source.Should().Be(QuestionSource.School);
        question.Categories.Should().Equal(LicenseCategory.A, LicenseCategory.B);
        question.Options.Select(option => option.Text).Should().Equal("Stop", "Give way", "Speed up");
        question.CorrectOption.Text.Should().Be("Give way");
        question.ImageKey.Should().BeNull();
    }

    [Fact]
    public async Task Create_OfficialQuestionByAnInstructor_ReturnsOfficialAdministratorOnly()
    {
        var result = await Create(_instructor, QuestionSource.Official);

        result.Error.Should().Be(ApplicationErrors.Question.OfficialAdministratorOnly);
        _questions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_OfficialQuestionByAnAdministrator_AddsIt()
    {
        var result = await Create(TestData.Administrator(), QuestionSource.Official, "1.12");

        result.IsSuccess.Should().BeTrue();
        _questions.Items.Should().ContainSingle().Which.ExternalCode.Should().Be("1.12");
    }

    [Fact]
    public async Task Create_ExternalCodeOfAnotherQuestionInOtherCase_ReturnsExternalCodeTaken()
    {
        Seed(externalCode: "A-12");

        var result = await Create(_instructor, externalCode: "a-12");

        result.Error.Should().Be(ApplicationErrors.Question.ExternalCodeTaken);
        _questions.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_TopicOfNoCourse_ReturnsCourseTopicNotFound()
    {
        var result = await Create(_instructor, topicId: Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<CourseTopic>());
    }

    [Fact]
    public async Task Update_SchoolQuestion_ReplacesTheContentAndKeepsTheImage()
    {
        var question = Seed();

        var result = await Update(question, _instructor);

        result.IsSuccess.Should().BeTrue();
        question.Text.Should().Be("Who goes first?");
        question.Categories.Should().Equal(LicenseCategory.C);
        question.CorrectOption.Text.Should().Be("The tram");
        question.ImageKey.Should().Be("images/q1.png");
    }

    [Fact]
    public async Task Update_OwnExternalCode_Succeeds()
    {
        var question = Seed(externalCode: "A-12");

        var result = await Update(question, _instructor, "A-12");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_OfficialQuestionByAnInstructor_ReturnsOfficialAdministratorOnly()
    {
        var question = Seed(QuestionSource.Official);

        var result = await Update(question, _instructor);

        result.Error.Should().Be(ApplicationErrors.Question.OfficialAdministratorOnly);
        question.Text.Should().Be("Old text");
    }

    [Fact]
    public async Task Update_TwoCorrectOptions_ReturnsNotExactlyOneCorrectOption()
    {
        var question = Seed();

        var result = await new UpdateQuestionCommandHandler(Access(_instructor), new QuestionContent(_questions, _courses)).Handle(
            new UpdateQuestionCommand(
                question.Id, "Text", null, _topicId, [LicenseCategory.B], null,
                [new AnswerOptionInput("One", true), new AnswerOptionInput("Two", true)]),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Question.NotExactlyOneCorrectOption);
    }

    [Fact]
    public async Task Deactivate_OfficialQuestionByAnAdministrator_TakesItOutOfNewTests()
    {
        var question = Seed(QuestionSource.Official);

        var result = await new DeactivateQuestionCommandHandler(Access(TestData.Administrator()))
            .Handle(new DeactivateQuestionCommand(question.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        question.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Reactivate_UnknownQuestion_ReturnsQuestionNotFound()
    {
        var result = await new ReactivateQuestionCommandHandler(Access(_instructor))
            .Handle(new ReactivateQuestionCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Question>());
    }

    [Fact]
    public void CreateValidator_NoCategoriesAndUndefinedSource_ReportsBoth()
    {
        var command = new CreateQuestionCommand("Text", null, _topicId, [], (QuestionSource)99, null, _options);

        var codes = new CreateQuestionCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.Question.SourceNotDefined.Code, DomainErrors.Question.NoCategories.Code);
    }

    [Fact]
    public void UpdateValidator_UndefinedCategory_ReportsCategoryNotDefined()
    {
        var command = new UpdateQuestionCommand(Guid.CreateVersion7(), "Text", null, _topicId, [(LicenseCategory)99], null, _options);

        var result = new UpdateQuestionCommandValidator().Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(DomainErrors.Question.CategoryNotDefined.Code);
    }

    [Fact]
    public async Task GetList_Filters_PassesThemWithTheTrimmedSearch()
    {
        var courseId = Guid.CreateVersion7();

        await new GetQuestionsQueryHandler(_queries).Handle(
            new GetQuestionsQuery(" sign ", courseId, _topicId, LicenseCategory.B, QuestionSource.Official, true, 2, 50),
            TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be((
            new QuestionListFilter("sign", courseId, _topicId, LicenseCategory.B, QuestionSource.Official, true), new PageRequest(2, 50)));
    }

    [Fact]
    public async Task Get_UnknownQuestion_ReturnsQuestionNotFound()
    {
        var result = await new GetQuestionQueryHandler(_queries)
            .Handle(new GetQuestionQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Question>());
    }
}
