using AutoElite.Application.Exams.CorrectExamResult;
using AutoElite.Application.Exams.RecordPracticeExamResult;
using AutoElite.Application.Exams.StartExamTest;
using AutoElite.Application.Exams.TestCompletion;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Exams;

public class ExamResultHandlerTests
{
    private static readonly FixedTimeProvider _duringTheExam = new(new DateTimeOffset(ExamFixture.ExamTime.AddMinutes(5)));

    private readonly ExamFixture _fixture = new();

    private Task<Result<Guid>> StartExamTest(Exam exam, FakeCurrentUser? caller = null, TimeProvider? clock = null, Guid? contractId = null)
    {
        var time = clock ?? _duringTheExam;

        return new StartExamTestCommandHandler(
                _fixture.Exams, _fixture.Groups, _fixture.Contracts, _fixture.Templates, _fixture.Sessions,
                _fixture.CreateAttempts(time), caller ?? _fixture.Student, time)
            .Handle(new StartExamTestCommand(exam.Id, contractId ?? _fixture.Member.Id), TestContext.Current.CancellationToken);
    }

    private async Task<TestSession> FinishedAttempt(Exam exam)
    {
        var result = await StartExamTest(exam);
        var session = _fixture.Sessions.Items.Single(candidate => candidate.Id == result.Value);
        foreach (var question in session.Questions.ToList())
            session.Answer(question.Id, question.CorrectOptionId, ExamFixture.ExamTime.AddMinutes(6));

        return session;
    }

    private Task RecordFromTheAttempt(TestSession session) =>
        new RecordExamResultWhenTestFinishedHandler(_fixture.Exams, _fixture.Groups, _fixture.Sessions).Handle(
            session.DomainEvents.OfType<TestSessionFinishedDomainEvent>().Single(), TestContext.Current.CancellationToken);

    private Task<Result> RecordPractice(Exam exam, FakeCurrentUser caller, bool passed = true) =>
        new RecordPracticeExamResultCommandHandler(_fixture.CreateAccess(caller), _fixture.Contracts, caller, _duringTheExam).Handle(
            new RecordPracticeExamResultCommand(exam.Id, _fixture.Member.Id, passed, 850, "Confident driving"),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task StartExamTest_MemberAfterTheStart_StartsAnAttemptOfTheExam()
    {
        var exam = _fixture.SeedExam();

        var result = await StartExamTest(exam);

        var session = _fixture.Sessions.Items.Should().ContainSingle().Which;
        session.Id.Should().Be(result.Value);
        session.ExamId.Should().Be(exam.Id);
        session.TemplateId.Should().Be(_fixture.Template.Id);
    }

    [Fact]
    public async Task StartExamTest_BeforeTheStart_ReturnsNotStarted()
    {
        var exam = _fixture.SeedExam();

        var result = await StartExamTest(exam, clock: TestData.Clock());

        result.Error.Should().Be(DomainErrors.Exam.NotStarted);
        _fixture.Sessions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task StartExamTest_SecondTime_ReturnsAttemptAlreadyTaken()
    {
        var exam = _fixture.SeedExam();
        await StartExamTest(exam);

        var result = await StartExamTest(exam);

        result.Error.Should().Be(ApplicationErrors.Exam.AttemptAlreadyTaken);
    }

    [Fact]
    public async Task StartExamTest_ContractOutsideTheGroup_ReturnsExamNotFound()
    {
        var exam = _fixture.SeedExam();
        var outsider = _fixture.SeedContract("outsider@example.com");
        var student = new FakeCurrentUser { UserId = outsider.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };

        var result = await StartExamTest(exam, student, contractId: outsider.Id);

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
    }

    [Fact]
    public async Task StartExamTest_PracticeExam_ReturnsWrongKind()
    {
        var exam = _fixture.SeedExam(ExamKind.Practice);

        var result = await StartExamTest(exam);

        result.Error.Should().Be(DomainErrors.Exam.WrongKind);
    }

    [Fact]
    public async Task AttemptFinished_RecordsTheTheoryResultOnce()
    {
        var exam = _fixture.SeedExam();
        var session = await FinishedAttempt(exam);

        await RecordFromTheAttempt(session);
        await RecordFromTheAttempt(session);

        var result = exam.Results.Should().ContainSingle().Which;
        result.ContractId.Should().Be(_fixture.Member.Id);
        result.Passed.Should().BeTrue();
        result.Score.Should().Be(2);
        result.TestSessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task RecordPractice_DrivingInstructor_RecordsTheResult()
    {
        var exam = _fixture.SeedExam(ExamKind.Practice);
        var instructor = FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

        var result = await RecordPractice(exam, instructor);

        result.IsSuccess.Should().BeTrue();
        var recorded = exam.Results.Should().ContainSingle().Which;
        recorded.Score.Should().Be(850);
        recorded.RecordedByUserId.Should().Be(instructor.UserId);
    }

    [Fact]
    public async Task RecordPractice_TheoryExam_ReturnsExamNotFound()
    {
        var exam = _fixture.SeedExam();

        var result = await RecordPractice(exam, FakeCurrentUser.SignedIn(UserRole.DrivingInstructor));

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
    }

    [Fact]
    public async Task Correct_TheoryResultByTheResponsibleInstructor_ChangesIt()
    {
        var exam = _fixture.SeedExam();
        await RecordFromTheAttempt(await FinishedAttempt(exam));

        var result = await new CorrectExamResultCommandHandler(
                _fixture.CreateAccess(_fixture.Instructor), _fixture.Instructor, _duringTheExam)
            .Handle(
                new CorrectExamResultCommand(exam.Id, _fixture.Member.Id, false, 1, "Prompting detected"),
                TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var corrected = exam.Results.Should().ContainSingle().Which;
        corrected.Passed.Should().BeFalse();
        corrected.CorrectedByUserId.Should().Be(_fixture.Instructor.UserId);
    }

    [Fact]
    public async Task Correct_TheoryResultByADrivingInstructor_ReturnsExamNotFound()
    {
        var exam = _fixture.SeedExam();
        var instructor = FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

        var result = await new CorrectExamResultCommandHandler(_fixture.CreateAccess(instructor), instructor, _duringTheExam)
            .Handle(new CorrectExamResultCommand(exam.Id, _fixture.Member.Id, true), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
    }
}
