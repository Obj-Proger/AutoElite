using AutoElite.Application.TheoryLessons.CancelTheoryLesson;
using AutoElite.Application.TheoryLessons.ChangeTheoryLessonInstructor;
using AutoElite.Application.TheoryLessons.RescheduleTheoryLesson;
using AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TheoryLessons;

public class TheoryLessonCommandHandlerTests
{
    private readonly TheoryLessonFixture _fixture = new();
    private static readonly DateTime _start = TheoryLessonFixture.LessonStart;

    private Task<Result<Guid>> Schedule(DateTime start, FakeCurrentUser? caller = null, Guid? instructorId = null) =>
        new ScheduleTheoryLessonCommandHandler(
                _fixture.Lessons, _fixture.Groups, _fixture.Courses, _fixture.CreateLessonInstructors(), caller ?? _fixture.Instructor,
                TestData.Clock())
            .Handle(
                new ScheduleTheoryLessonCommand(_fixture.Group.Id, _fixture.TopicId, start, start.AddHours(2), instructorId),
                TestContext.Current.CancellationToken);

    [Fact]
    public async Task Schedule_ResponsibleInstructor_SchedulesTheLessonGivenByThemselves()
    {
        var result = await Schedule(_start);

        var lesson = _fixture.Lessons.Items.Should().ContainSingle().Which;
        lesson.Id.Should().Be(result.Value);
        lesson.InstructorId.Should().Be(_fixture.Instructor.UserId!.Value);
        lesson.TopicId.Should().Be(_fixture.TopicId);
        lesson.Time.StartUtc.Should().Be(_start);
    }

    [Fact]
    public async Task Schedule_AnotherInstructorsGroup_ReturnsStudyGroupNotFound()
    {
        var stranger = _fixture.SeedInstructor("stranger@example.com");

        var result = await Schedule(_start, stranger);

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task Schedule_Substitute_ChecksTheRoleOfTheSubstitute()
    {
        var driver = _fixture.SeedInstructor("driver@example.com", UserRole.DrivingInstructor);

        var result = await Schedule(_start, instructorId: driver.UserId);

        result.Error.Should().Be(ApplicationErrors.StudyGroup.NotATheoryInstructor);
    }

    [Fact]
    public async Task Schedule_OverlappingLessonOfTheGroup_ReturnsTimeConflict()
    {
        _fixture.SeedLesson();

        var result = await Schedule(_start.AddHours(1));

        result.Error.Should().Be(ApplicationErrors.TheoryLesson.TimeConflict);
    }

    [Fact]
    public async Task Schedule_RightAfterAnotherLesson_Succeeds()
    {
        _fixture.SeedLesson();

        var result = await Schedule(_start.AddHours(2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Schedule_InThePast_ReturnsStartInPast()
    {
        var result = await Schedule(TestData.Now.AddHours(-1));

        result.Error.Should().Be(DomainErrors.TheoryLesson.StartInPast);
    }

    [Fact]
    public async Task Reschedule_FreeTime_MovesTheLesson()
    {
        var lesson = _fixture.SeedLesson();
        var newStart = _start.AddDays(1);

        var result = await new RescheduleTheoryLessonCommandHandler(
                _fixture.CreateLessonAccess(_fixture.Instructor), _fixture.Lessons, TestData.Clock())
            .Handle(new RescheduleTheoryLessonCommand(lesson.Id, newStart, newStart.AddHours(2)), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        lesson.Time.StartUtc.Should().Be(newStart);
    }

    [Fact]
    public async Task Reschedule_ByAStranger_ReturnsTheoryLessonNotFound()
    {
        var lesson = _fixture.SeedLesson();
        var stranger = _fixture.SeedInstructor("stranger@example.com");

        var result = await new RescheduleTheoryLessonCommandHandler(_fixture.CreateLessonAccess(stranger), _fixture.Lessons, TestData.Clock())
            .Handle(new RescheduleTheoryLessonCommand(lesson.Id, _start, _start.AddHours(1)), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<TheoryLesson>());
    }

    [Fact]
    public async Task ChangeInstructor_FreeSubstitute_HandsOverTheLesson()
    {
        var lesson = _fixture.SeedLesson();
        var substitute = _fixture.SeedInstructor("substitute@example.com");

        var result = await new ChangeTheoryLessonInstructorCommandHandler(
                _fixture.CreateLessonAccess(_fixture.Instructor), _fixture.Lessons, _fixture.CreateLessonInstructors())
            .Handle(new ChangeTheoryLessonInstructorCommand(lesson.Id, substitute.UserId!.Value), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        lesson.InstructorId.Should().Be(substitute.UserId.Value);
    }

    [Fact]
    public async Task ChangeInstructor_SubstituteBusyElsewhere_ReturnsTimeConflict()
    {
        var lesson = _fixture.SeedLesson();
        var substitute = _fixture.SeedInstructor("substitute@example.com");
        var otherGroup = StudyGroup.Create("B-2026-04", _fixture.Course, substitute.UserId!.Value, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        _fixture.Groups.Seed(otherGroup);
        _fixture.Lessons.Seed(TheoryLesson.Schedule(
                otherGroup, _fixture.Course, _fixture.TopicId, substitute.UserId.Value,
                TimeInterval.Create(_start.AddMinutes(30), _start.AddHours(1)).Value, TestData.Now)
            .Value);

        var result = await new ChangeTheoryLessonInstructorCommandHandler(
                _fixture.CreateLessonAccess(_fixture.Instructor), _fixture.Lessons, _fixture.CreateLessonInstructors())
            .Handle(new ChangeTheoryLessonInstructorCommand(lesson.Id, substitute.UserId.Value), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.TheoryLesson.TimeConflict);
        lesson.InstructorId.Should().Be(_fixture.Instructor.UserId!.Value);
    }

    [Fact]
    public async Task Cancel_ScheduledLesson_CancelsItWithTheReason()
    {
        var lesson = _fixture.SeedLesson();

        var result = await new CancelTheoryLessonCommandHandler(_fixture.CreateLessonAccess(_fixture.Instructor), TestData.Clock())
            .Handle(new CancelTheoryLessonCommand(lesson.Id, "Instructor is ill"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        lesson.Status.Should().Be(TheoryLessonStatus.Cancelled);
        lesson.CancellationReason.Should().Be("Instructor is ill");
    }
}
