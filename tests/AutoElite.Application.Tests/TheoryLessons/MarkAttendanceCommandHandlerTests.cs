using AutoElite.Application.TheoryLessons.MarkAttendance;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TheoryLessons;

public class MarkAttendanceCommandHandlerTests
{
    private readonly TheoryLessonFixture _fixture = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(TheoryLessonFixture.LessonStart.AddMinutes(10)));

    private Task<Result> Mark(Guid lessonId, params AttendanceMark[] marks) =>
        new MarkAttendanceCommandHandler(_fixture.CreateLessonAccess(_fixture.Instructor), _fixture.Contracts, _fixture.Instructor, _clock)
            .Handle(new MarkAttendanceCommand(lessonId, marks), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Mark_WholeGroup_CountsPresentStudentsAndCompletesTheLesson()
    {
        var present = _fixture.SeedMember();
        var absent = _fixture.SeedMember();
        var lesson = _fixture.SeedLesson();

        var result = await Mark(lesson.Id, new AttendanceMark(present.Id, true), new AttendanceMark(absent.Id, false, "Ill"));

        result.IsSuccess.Should().BeTrue();
        present.AttendedTheoryLessons.Should().Be(1);
        absent.AttendedTheoryLessons.Should().Be(0);
        lesson.Status.Should().Be(TheoryLessonStatus.Completed);
        lesson.Attendance.Should().HaveCount(2);
    }

    [Fact]
    public async Task Mark_CorrectionToAbsent_TakesTheCountBack()
    {
        var student = _fixture.SeedMember();
        var lesson = _fixture.SeedLesson();
        await Mark(lesson.Id, new AttendanceMark(student.Id, true));

        await Mark(lesson.Id, new AttendanceMark(student.Id, false));

        student.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public async Task Mark_ContractOutsideTheGroup_ReturnsNotGroupMember()
    {
        var lesson = _fixture.SeedLesson();

        var result = await Mark(lesson.Id, new AttendanceMark(Guid.CreateVersion7(), true));

        result.Error.Should().Be(DomainErrors.TheoryLesson.NotGroupMember);
    }

    [Fact]
    public async Task Mark_BeforeTheLessonStarts_ReturnsNotStarted()
    {
        var student = _fixture.SeedMember();
        var lesson = _fixture.SeedLesson(TheoryLessonFixture.LessonStart.AddDays(1));

        var result = await Mark(lesson.Id, new AttendanceMark(student.Id, true));

        result.Error.Should().Be(DomainErrors.TheoryLesson.NotStarted);
        student.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public void Validator_NoMarksOrRepeatedContract_ReportsTheError()
    {
        var validator = new MarkAttendanceCommandValidator();
        var contractId = Guid.CreateVersion7();

        validator.Validate(new MarkAttendanceCommand(Guid.CreateVersion7(), [])).Errors
            .Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.TheoryLesson.MarksEmpty.Code);
        validator.Validate(new MarkAttendanceCommand(Guid.CreateVersion7(), [new(contractId, true), new(contractId, false)])).Errors
            .Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.TheoryLesson.DuplicateMark.Code);
    }
}
