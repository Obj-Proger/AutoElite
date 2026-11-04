using AutoElite.Application.TestTemplates;
using AutoElite.Application.TestTemplates.CreateTestTemplate;
using AutoElite.Application.TestTemplates.DeactivateTestTemplate;
using AutoElite.Application.TestTemplates.GetTestTemplate;
using AutoElite.Application.TestTemplates.GetTestTemplates;
using AutoElite.Application.TestTemplates.UpdateTestTemplate;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestTemplates;

public class TestTemplateHandlerTests
{
    private readonly InMemoryRepository<TestTemplate> _templates = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeTestTemplateQueries _queries = new();
    private readonly Guid _signsTopicId;
    private readonly Guid _markingsTopicId;

    public TestTemplateHandlerTests()
    {
        var course = TestData.NewCourse();
        _signsTopicId = course.AddTopic("Road signs", null).Value.Id;
        _markingsTopicId = course.AddTopic("Road markings", null).Value.Id;
        _courses.Seed(course);
    }

    private static CreateTestTemplateCommand TrafficPoliceExam(params TestBlockInput[] blocks) =>
        new("Exam B", LicenseCategory.B, 20, 20, 2, 1, 5, 5, blocks, QuestionSource.Official);

    private Task<Result<Guid>> Create(CreateTestTemplateCommand command) =>
        new CreateTestTemplateCommandHandler(_templates, new TestTemplateContent(_courses))
            .Handle(command, TestContext.Current.CancellationToken);

    private TestTemplate Seed()
    {
        var template = TestTemplate.Create(
                "Practice", LicenseCategory.B, TestRules.TrafficPoliceExam, [TestBlock.Create(20, []).Value], null)
            .Value;
        _templates.Seed(template);

        return template;
    }

    private static TestTemplateDetails Details(bool isActive) =>
        new(Guid.CreateVersion7(), "Exam B", LicenseCategory.B, 20, 20, 2, 1, 5, 5, null, isActive, []);

    [Fact]
    public async Task Create_TrafficPoliceExamInTwoBlocks_CreatesTheTemplate()
    {
        var result = await Create(TrafficPoliceExam(new TestBlockInput(10, [_signsTopicId]), new TestBlockInput(10, [_markingsTopicId])));

        var template = _templates.Items.Should().ContainSingle().Which;
        template.Id.Should().Be(result.Value);
        template.Rules.Should().Be(TestRules.TrafficPoliceExam);
        template.Blocks.Should().HaveCount(2);
        template.Blocks[0].TopicIds.Should().Equal(_signsTopicId);
        template.Source.Should().Be(QuestionSource.Official);
    }

    [Fact]
    public async Task Create_SingleBlockOfAllTopics_Succeeds()
    {
        var result = await Create(TrafficPoliceExam(new TestBlockInput(20, null)));

        result.IsSuccess.Should().BeTrue();
        _templates.Items.Should().ContainSingle().Which.Blocks.Should().ContainSingle().Which.CoversAllTopics.Should().BeTrue();
    }

    [Fact]
    public async Task Create_BlocksDoNotAddUpToTheQuestionCount_ReturnsBlocksDoNotMatchQuestionCount()
    {
        var result = await Create(TrafficPoliceExam(new TestBlockInput(10, [_signsTopicId])));

        result.Error.Should().Be(DomainErrors.TestTemplate.BlocksDoNotMatchQuestionCount);
        _templates.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_TopicOfNoCourse_ReturnsCourseTopicNotFound()
    {
        var result = await Create(TrafficPoliceExam(new TestBlockInput(20, [Guid.CreateVersion7()])));

        result.Error.Should().Be(ApplicationErrors.NotFound<CourseTopic>());
    }

    [Fact]
    public async Task Create_TimeLimitOverThreeHours_ReturnsTimeLimitOutOfRange()
    {
        var command = TrafficPoliceExam(new TestBlockInput(20, null)) with { TimeLimitMinutes = 181 };

        var result = await Create(command);

        result.Error.Should().Be(DomainErrors.TestRules.TimeLimitOutOfRange);
    }

    [Fact]
    public async Task Update_NewRulesAndBlocks_AppliesThem()
    {
        var template = Seed();

        var result = await new UpdateTestTemplateCommandHandler(_templates, new TestTemplateContent(_courses)).Handle(
            new UpdateTestTemplateCommand(
                template.Id, "Signs drill", LicenseCategory.B, 10, 15, 1, null, 0, 0, [new TestBlockInput(10, [_signsTopicId])]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        template.Name.Should().Be("Signs drill");
        template.Rules.QuestionCount.Should().Be(10);
        template.Rules.TimeLimit.Should().Be(TimeSpan.FromMinutes(15));
        template.Rules.HasAddOnQuestions.Should().BeFalse();
        template.Blocks.Should().ContainSingle().Which.TopicIds.Should().Equal(_signsTopicId);
    }

    [Fact]
    public async Task Deactivate_ActiveTemplate_StopsOfferingIt()
    {
        var template = Seed();

        var result = await new DeactivateTestTemplateCommandHandler(_templates)
            .Handle(new DeactivateTestTemplateCommand(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_UndefinedCategoryAndSourceWithoutBlocks_ReportsEach()
    {
        var command = new CreateTestTemplateCommand("Exam", (LicenseCategory)99, 20, 20, 2, 1, 5, 5, [], (QuestionSource)99);

        var codes = new CreateTestTemplateCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(
            DomainErrors.TestTemplate.CategoryNotDefined.Code, DomainErrors.TestTemplate.SourceNotDefined.Code,
            DomainErrors.TestTemplate.NoBlocks.Code);
    }

    [Fact]
    public async Task GetList_Student_ReadsOnlyActiveTemplates()
    {
        await new GetTestTemplatesQueryHandler(_queries, FakeCurrentUser.SignedIn(UserRole.Student))
            .Handle(new GetTestTemplatesQuery(LicenseCategory.B, IsActive: false), TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be(((LicenseCategory?)LicenseCategory.B, (bool?)true));
    }

    [Fact]
    public async Task GetList_Instructor_PassesTheActivityFilter()
    {
        await new GetTestTemplatesQueryHandler(_queries, FakeCurrentUser.SignedIn(UserRole.TheoryInstructor))
            .Handle(new GetTestTemplatesQuery(IsActive: false), TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be(((LicenseCategory?)null, (bool?)false));
    }

    [Fact]
    public async Task Get_DeactivatedTemplateByAStudent_ReturnsTestTemplateNotFound()
    {
        var details = Details(isActive: false);
        _queries.Seed(details);

        var result = await new GetTestTemplateQueryHandler(_queries, FakeCurrentUser.SignedIn(UserRole.Student))
            .Handle(new GetTestTemplateQuery(details.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<TestTemplate>());
    }

    [Fact]
    public async Task Get_DeactivatedTemplateByAnAdministrator_ReadsTheCard()
    {
        var details = Details(isActive: false);
        _queries.Seed(details);

        var result = await new GetTestTemplateQueryHandler(_queries, TestData.Administrator())
            .Handle(new GetTestTemplateQuery(details.Id), TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(details);
    }
}
