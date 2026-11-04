using AutoElite.Application.Exams.CancelExam;
using AutoElite.Application.Exams.RescheduleExam;
using AutoElite.Application.Exams.ScheduleExam;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Exams;

public class ExamScheduleHandlerTests
{
    private static readonly DateTime _time = ExamFixture.ExamTime;

    private readonly ExamFixture _fixture = new();

    private Task<Result<Guid>> Schedule(ExamKind kind, Guid? templateId, FakeCurrentUser? caller = null, DateTime? time = null) =>
        new ScheduleExamCommandHandler(
                _fixture.Exams, _fixture.Groups, _fixture.Templates, caller ?? _fixture.Instructor, TestData.Clock())
            .Handle(new ScheduleExamCommand(_fixture.Group.Id, kind, time ?? _time, templateId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Schedule_TheoryExamWithATemplate_SchedulesItAndNotifiesTheGroup()
    {
        var result = await Schedule(ExamKind.Theory, _fixture.Template.Id);

        var exam = _fixture.Exams.Items.Should().ContainSingle().Which;
        exam.Id.Should().Be(result.Value);
        exam.TestTemplateId.Should().Be(_fixture.Template.Id);
        exam.ScheduledAtUtc.Should().Be(_time);
        exam.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ExamScheduledDomainEvent);
    }

    [Fact]
    public async Task Schedule_PracticeExamWithoutATemplate_Succeeds()
    {
        var result = await Schedule(ExamKind.Practice, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Schedule_TheoryExamWithoutATemplate_ReturnsTemplateRequired()
    {
        var result = await Schedule(ExamKind.Theory, null);

        result.Error.Should().Be(DomainErrors.Exam.TemplateRequired);
    }

    [Fact]
    public async Task Schedule_UnknownTemplate_ReturnsTestTemplateNotFound()
    {
        var result = await Schedule(ExamKind.Theory, Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<TestTemplate>());
    }

    [Fact]
    public async Task Schedule_GroupOfAnotherInstructor_ReturnsStudyGroupNotFound()
    {
        var result = await Schedule(ExamKind.Practice, null, FakeCurrentUser.SignedIn(UserRole.TheoryInstructor));

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
        _fixture.Exams.Items.Should().BeEmpty();
    }

    [Fact]
    public void ScheduleValidator_UndefinedKindAndLocalTime_ReportsBoth()
    {
        var command = new ScheduleExamCommand(_fixture.Group.Id, (ExamKind)99, DateTime.SpecifyKind(_time, DateTimeKind.Local));

        var codes = new ScheduleExamCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.Exam.KindNotDefined.Code, DomainErrors.TimeInterval.NotUtc.Code);
    }

    [Fact]
    public async Task Reschedule_ByTheResponsibleInstructor_MovesTheExam()
    {
        var exam = _fixture.SeedExam();
        var newTime = _time.AddDays(1);

        var result = await new RescheduleExamCommandHandler(_fixture.CreateAccess(_fixture.Instructor), TestData.Clock())
            .Handle(new RescheduleExamCommand(exam.Id, newTime), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        exam.ScheduledAtUtc.Should().Be(newTime);
    }

    [Fact]
    public async Task Reschedule_ByAnotherInstructor_ReturnsExamNotFound()
    {
        var exam = _fixture.SeedExam();

        var result = await new RescheduleExamCommandHandler(
                _fixture.CreateAccess(FakeCurrentUser.SignedIn(UserRole.TheoryInstructor)), TestData.Clock())
            .Handle(new RescheduleExamCommand(exam.Id, _time.AddDays(1)), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
        exam.ScheduledAtUtc.Should().Be(_time);
    }

    [Fact]
    public async Task Cancel_ScheduledExam_CancelsItWithTheReason()
    {
        var exam = _fixture.SeedExam(ExamKind.Practice);

        var result = await new CancelExamCommandHandler(_fixture.CreateAccess(_fixture.Instructor), TestData.Clock())
            .Handle(new CancelExamCommand(exam.Id, "Examiner is ill"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        exam.Status.Should().Be(ExamStatus.Cancelled);
        exam.CancellationReason.Should().Be("Examiner is ill");
    }
}
