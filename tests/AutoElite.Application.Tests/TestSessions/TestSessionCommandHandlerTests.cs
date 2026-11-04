using AutoElite.Application.TestSessions;
using AutoElite.Application.TestSessions.AnswerTestQuestion;
using AutoElite.Application.TestSessions.StartTest;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestSessions;

public class TestSessionCommandHandlerTests
{
    private readonly FakeTestSessionRepository _sessions = new();
    private readonly FakeContractRepository _contracts = new();
    private readonly InMemoryRepository<TestTemplate> _templates = new();
    private readonly FakeQuestionPicker _picker = new();
    private readonly Course _course = TestData.NewCourse();
    private readonly Guid _signsTopicId;
    private readonly Guid _markingsTopicId;
    private readonly Contract _contract;
    private readonly FakeCurrentUser _student;
    private readonly TestTemplate _template;

    public TestSessionCommandHandlerTests()
    {
        _signsTopicId = _course.AddTopic("Road signs", null).Value.Id;
        _markingsTopicId = _course.AddTopic("Road markings", null).Value.Id;

        // Two blocks of two questions, one mistake allowed per test and block, one question and one minute added per mistake.
        var rules = TestRules.Create(4, TimeSpan.FromMinutes(10), 1, 1, 1, TimeSpan.FromMinutes(1)).Value;
        _template = TestTemplate.Create(
                "Practice", LicenseCategory.B, rules,
                [TestBlock.Create(2, [_signsTopicId]).Value, TestBlock.Create(2, [_markingsTopicId]).Value], null)
            .Value;
        _templates.Seed(_template);

        for (var index = 0; index < 4; index++)
        {
            SeedQuestion(_signsTopicId, $"Sign {index}");
            SeedQuestion(_markingsTopicId, $"Marking {index}");
        }

        _contract = Contract.Create(
                "AE-1", TestData.NewUser(), _course, _course.Price, Transmission.Manual, null, new DateOnly(2026, 2, 1),
                new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        _contracts.Seed(_contract);
        _student = new FakeCurrentUser { UserId = _contract.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };
    }

    private SessionQuestions Questions() => new(_picker);

    private void SeedQuestion(Guid topicId, string text) =>
        _picker.Bank.Add(Question.Create(
                text, null, null, topicId, [LicenseCategory.B], QuestionSource.School, null,
                [new AnswerOptionDraft("Right", true), new AnswerOptionDraft("Wrong", false)])
            .Value);

    private Task<Result<Guid>> Start(FakeCurrentUser? caller = null, DateTime? now = null) =>
        new StartTestCommandHandler(
                _sessions, _contracts, _templates, Questions(), caller ?? _student,
                new FixedTimeProvider(new DateTimeOffset(now ?? TestData.Now)))
            .Handle(new StartTestCommand(_contract.Id, _template.Id), TestContext.Current.CancellationToken);

    private Task<Result<TestSessionStatus>> Answer(TestSession session, SessionQuestion question, bool correctly, DateTime? now = null)
    {
        var optionId = correctly
            ? question.CorrectOptionId
            : question.Options.First(option => option.OptionId != question.CorrectOptionId).OptionId;

        return new AnswerTestQuestionCommandHandler(
                _sessions, _templates, Questions(), _student,
                new FixedTimeProvider(new DateTimeOffset(now ?? TestData.Now.AddMinutes(1))))
            .Handle(new AnswerTestQuestionCommand(session.Id, question.Id, optionId), TestContext.Current.CancellationToken);
    }

    private async Task<TestSession> StartedSession()
    {
        var result = await Start();

        return _sessions.Items.Single(session => session.Id == result.Value);
    }

    [Fact]
    public async Task Start_ActiveContractAndTemplate_DrawsTheQuestionsOfEachBlock()
    {
        var session = await StartedSession();

        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.ExamId.Should().BeNull();
        session.Questions.Select(question => question.Text).Should().Equal("Sign 0", "Sign 1", "Marking 0", "Marking 1");
        session.DeadlineUtc.Should().Be(TestData.Now.AddMinutes(10));
    }

    [Fact]
    public async Task Start_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var result = await Start(FakeCurrentUser.SignedIn(UserRole.Student));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
        _sessions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_AttemptInProgress_ReturnsAlreadyInProgress()
    {
        await Start();

        var result = await Start(now: TestData.Now.AddMinutes(5));

        result.Error.Should().Be(ApplicationErrors.TestSession.AlreadyInProgress);
    }

    [Fact]
    public async Task Start_AttemptWhoseTimeHasRunOut_ClosesItAndStartsANewOne()
    {
        var first = await StartedSession();

        var result = await Start(now: TestData.Now.AddMinutes(11));

        result.IsSuccess.Should().BeTrue();
        first.Status.Should().Be(TestSessionStatus.Failed);
        first.FailureReason.Should().Be(TestFailureReason.TimeExpired);
        _sessions.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Start_BankTooSmallForABlock_ReturnsNotEnoughQuestions()
    {
        _picker.Bank.RemoveAll(question => question.TopicId == _markingsTopicId);

        var result = await Start();

        result.Error.Should().Be(ApplicationErrors.TestSession.NotEnoughQuestions);
    }

    [Fact]
    public async Task Answer_AllCorrect_PassesTheAttempt()
    {
        var session = await StartedSession();
        var statuses = new List<TestSessionStatus>();

        foreach (var question in session.Questions.ToList())
            statuses.Add((await Answer(session, question, correctly: true)).Value);

        statuses.Should().Equal(
            TestSessionStatus.InProgress, TestSessionStatus.InProgress, TestSessionStatus.InProgress, TestSessionStatus.Passed);
    }

    [Fact]
    public async Task Answer_AllowedMistake_AddsAQuestionFromTheSameBlockNotShownBefore()
    {
        var session = await StartedSession();

        var result = await Answer(session, session.Questions[0], correctly: false);

        result.Value.Should().Be(TestSessionStatus.InProgress);
        var added = session.Questions.Should().HaveCount(5).And.Subject.Last();
        added.IsAddOn.Should().BeTrue();
        added.BlockNumber.Should().Be(1);
        added.Text.Should().Be("Sign 2");
        session.DeadlineUtc.Should().Be(TestData.Now.AddMinutes(11));
    }

    [Fact]
    public async Task Answer_MistakeInAnAddedQuestion_FailsTheAttempt()
    {
        var session = await StartedSession();
        await Answer(session, session.Questions[0], correctly: false);

        var result = await Answer(session, session.Questions[session.Questions.Count - 1], correctly: false);

        result.Value.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.MistakeInAddOnQuestion);
    }

    [Fact]
    public async Task Answer_NoQuestionLeftToAdd_ReturnsNotEnoughQuestions()
    {
        var session = await StartedSession();
        _picker.Bank.RemoveAll(question =>
            question.TopicId == _signsTopicId && !session.Questions.Any(shown => shown.QuestionId == question.Id));

        var result = await Answer(session, session.Questions[0], correctly: false);

        result.Error.Should().Be(ApplicationErrors.TestSession.NotEnoughQuestions);
    }

    [Fact]
    public async Task Answer_AfterTheDeadline_ClosesTheAttemptWithoutRecordingTheAnswer()
    {
        var session = await StartedSession();

        var result = await Answer(session, session.Questions[0], correctly: true, now: TestData.Now.AddMinutes(10));

        result.Value.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.TimeExpired);
        session.Questions[0].IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task Answer_AttemptOfAnotherStudent_ReturnsTestSessionNotFound()
    {
        var session = await StartedSession();
        var question = session.Questions[0];

        var result = await new AnswerTestQuestionCommandHandler(
                _sessions, _templates, Questions(), FakeCurrentUser.SignedIn(UserRole.Student), TestData.Clock())
            .Handle(
                new AnswerTestQuestionCommand(session.Id, question.Id, question.CorrectOptionId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<TestSession>());
        question.IsAnswered.Should().BeFalse();
    }
}
