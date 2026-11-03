namespace AutoElite.Domain.Tests.Entities;

public class TheoryLessonTests
{
    private static readonly DateTime _now = new(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _lessonStart = new(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _duringLesson = _lessonStart.AddMinutes(30);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _instructorId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _substituteId = new("44444444-4444-4444-4444-444444444444");
    private static readonly Currency _rub = Currency.Create("RUB").Value;

    private readonly Course _course;
    private readonly Guid _topicId;
    private readonly StudyGroup _group;

    public TheoryLessonTests()
    {
        _course = CreateCourse("Category B");
        _topicId = _course.AddTopic("Road signs", null).Value.Id;
        _group = StudyGroup.Create("B-2026-02", _course, _instructorId, null, _start, _end, 20).Value;
    }

    private static Course CreateCourse(string name) =>
        Course.Create(name, LicenseCategory.B, Money.Create(45000m, _rub).Value, 134, 56, null).Value;

    private static TimeInterval Interval(DateTime start, double hours = 2) =>
        TimeInterval.Create(start, start.AddHours(hours)).Value;

    private Contract EnrollStudent()
    {
        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;
        var contract = Contract.Create("AE-1", student, _course, Money.Create(45000m, _rub).Value, Transmission.Manual, null, _start, _end, _now).Value;
        _group.Enroll(contract, _now);

        return contract;
    }

    private TheoryLesson ScheduleLesson() =>
        TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_lessonStart), _now).Value;

    [Fact]
    public void Schedule_ValidData_ReturnsScheduledLessonWithGivenValues()
    {
        var result = TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_lessonStart), _now);

        result.IsSuccess.Should().BeTrue();
        var lesson = result.Value;
        lesson.GroupId.Should().Be(_group.Id);
        lesson.TopicId.Should().Be(_topicId);
        lesson.InstructorId.Should().Be(_instructorId);
        lesson.Time.Should().Be(Interval(_lessonStart));
        lesson.Status.Should().Be(TheoryLessonStatus.Scheduled);
        lesson.Attendance.Should().BeEmpty();
    }

    [Fact]
    public void Schedule_InactiveGroup_ReturnsGroupInactiveError()
    {
        _group.Deactivate();

        var result = TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_lessonStart), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.GroupInactive);
    }

    [Fact]
    public void Schedule_CourseOfAnotherGroup_ReturnsCourseMismatchError()
    {
        var otherCourse = CreateCourse("Category A");
        var otherTopicId = otherCourse.AddTopic("Road signs", null).Value.Id;

        var result = TheoryLesson.Schedule(_group, otherCourse, otherTopicId, _instructorId, Interval(_lessonStart), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.CourseMismatch);
    }

    [Fact]
    public void Schedule_TopicOfAnotherCourse_ReturnsTopicNotInCourseError()
    {
        var result = TheoryLesson.Schedule(_group, _course, Guid.CreateVersion7(), _instructorId, Interval(_lessonStart), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.TopicNotInCourse);
    }

    [Fact]
    public void Schedule_EmptyInstructorId_ReturnsInstructorIdEmptyError()
    {
        var result = TheoryLesson.Schedule(_group, _course, _topicId, Guid.Empty, Interval(_lessonStart), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.InstructorIdEmpty);
    }

    [Fact]
    public void Schedule_StartAtCurrentTime_ReturnsStartInPastError()
    {
        var result = TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_now), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.StartInPast);
    }

    [Fact]
    public void Schedule_DurationAtMaximum_ReturnsLesson()
    {
        var result = TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_lessonStart, 8), _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Schedule_DurationAboveMaximum_ReturnsDurationTooLongError()
    {
        var result = TheoryLesson.Schedule(_group, _course, _topicId, _instructorId, Interval(_lessonStart, 8.25), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.DurationTooLong);
    }

    [Fact]
    public void Reschedule_ScheduledLesson_MovesItAndRaisesRescheduledEvent()
    {
        var lesson = ScheduleLesson();
        var newTime = Interval(_lessonStart.AddDays(1));

        var result = lesson.Reschedule(newTime, _now);

        result.IsSuccess.Should().BeTrue();
        lesson.Time.Should().Be(newTime);
        lesson.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new TheoryLessonRescheduledDomainEvent(lesson.Id, _group.Id, newTime.StartUtc, newTime.EndUtc, _now));
    }

    [Fact]
    public void Reschedule_IntoThePast_ReturnsStartInPastError()
    {
        var lesson = ScheduleLesson();

        var result = lesson.Reschedule(Interval(_now.AddHours(-1)), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.StartInPast);
        lesson.Time.Should().Be(Interval(_lessonStart));
    }

    [Fact]
    public void Reschedule_LessonWithAttendance_ReturnsAttendanceAlreadyMarkedError()
    {
        var contract = EnrollStudent();
        EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        var result = lesson.Reschedule(Interval(_lessonStart.AddDays(1)), _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.AttendanceAlreadyMarked);
    }

    [Fact]
    public void ChangeInstructor_ScheduledLesson_ChangesInstructor()
    {
        var lesson = ScheduleLesson();

        var result = lesson.ChangeInstructor(_substituteId);

        result.IsSuccess.Should().BeTrue();
        lesson.InstructorId.Should().Be(_substituteId);
    }

    [Fact]
    public void ChangeInstructor_EmptyId_ReturnsInstructorIdEmptyError()
    {
        var lesson = ScheduleLesson();

        var result = lesson.ChangeInstructor(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.InstructorIdEmpty);
    }

    [Fact]
    public void Cancel_ScheduledLesson_CancelsAndRaisesCancelledEvent()
    {
        var lesson = ScheduleLesson();

        var result = lesson.Cancel("  Instructor is ill  ", _now);

        result.IsSuccess.Should().BeTrue();
        lesson.Status.Should().Be(TheoryLessonStatus.Cancelled);
        lesson.CancellationReason.Should().Be("Instructor is ill");
        lesson.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new TheoryLessonCancelledDomainEvent(lesson.Id, _group.Id, _lessonStart, "Instructor is ill", _now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_EmptyReason_ReturnsCancellationReasonEmptyError(string? reason)
    {
        var lesson = ScheduleLesson();

        var result = lesson.Cancel(reason, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.CancellationReasonEmpty);
        lesson.Status.Should().Be(TheoryLessonStatus.Scheduled);
    }

    [Fact]
    public void Cancel_CancelledLesson_ReturnsNotScheduledError()
    {
        var lesson = ScheduleLesson();
        lesson.Cancel("Instructor is ill", _now);

        var result = lesson.Cancel("Again", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.NotScheduled);
    }

    [Fact]
    public void Cancel_LessonWithAttendance_ReturnsAttendanceAlreadyMarkedError()
    {
        var contract = EnrollStudent();
        EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        var result = lesson.Cancel("Instructor is ill", _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.AttendanceAlreadyMarked);
    }

    [Fact]
    public void MarkAttendance_Present_RecordsMarkAndCountsLessonOnContract()
    {
        var contract = EnrollStudent();
        EnrollStudent();
        var lesson = ScheduleLesson();

        var result = lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        result.IsSuccess.Should().BeTrue();
        var mark = lesson.Attendance.Should().ContainSingle().Which;
        mark.ContractId.Should().Be(contract.Id);
        mark.StudentId.Should().Be(contract.StudentId);
        mark.IsPresent.Should().BeTrue();
        mark.MarkedByUserId.Should().Be(_instructorId);
        mark.MarkedAtUtc.Should().Be(_duringLesson);
        contract.AttendedTheoryLessons.Should().Be(1);
        lesson.Status.Should().Be(TheoryLessonStatus.Scheduled);
    }

    [Fact]
    public void MarkAttendance_Absent_RecordsMarkWithoutCountingLesson()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();

        lesson.MarkAttendance(_group, contract, false, "  Ill  ", _instructorId, _duringLesson);

        lesson.Attendance[0].IsPresent.Should().BeFalse();
        lesson.Attendance[0].Comment.Should().Be("Ill");
        contract.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public void MarkAttendance_LastCurrentMember_CompletesLesson()
    {
        var first = EnrollStudent();
        var second = EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.MarkAttendance(_group, first, true, null, _instructorId, _duringLesson);

        lesson.MarkAttendance(_group, second, false, null, _instructorId, _duringLesson);

        lesson.Status.Should().Be(TheoryLessonStatus.Completed);
    }

    [Fact]
    public void MarkAttendance_WithdrawnMemberWithoutMark_DoesNotBlockCompletion()
    {
        var first = EnrollStudent();
        var withdrawn = EnrollStudent();
        _group.Withdraw(withdrawn.Id, _now);
        var lesson = ScheduleLesson();

        lesson.MarkAttendance(_group, first, true, null, _instructorId, _duringLesson);

        lesson.Status.Should().Be(TheoryLessonStatus.Completed);
    }

    [Fact]
    public void MarkAttendance_CorrectPresentToAbsent_UpdatesMarkAndTakesLessonOffContract()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        var result = lesson.MarkAttendance(_group, contract, false, "Left early", _substituteId, _duringLesson.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        var mark = lesson.Attendance.Should().ContainSingle().Which;
        mark.IsPresent.Should().BeFalse();
        mark.Comment.Should().Be("Left early");
        mark.MarkedByUserId.Should().Be(_substituteId);
        contract.AttendedTheoryLessons.Should().Be(0);
        lesson.Status.Should().Be(TheoryLessonStatus.Completed);
    }

    [Fact]
    public void MarkAttendance_SameMarkTwice_CountsLessonOnce()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        lesson.MarkAttendance(_group, contract, true, "Late", _instructorId, _duringLesson);

        contract.AttendedTheoryLessons.Should().Be(1);
        lesson.Attendance[0].Comment.Should().Be("Late");
    }

    [Fact]
    public void MarkAttendance_BeforeLessonStarts_ReturnsNotStartedError()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();

        var result = lesson.MarkAttendance(_group, contract, true, null, _instructorId, _lessonStart.AddTicks(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.NotStarted);
        contract.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public void MarkAttendance_CancelledLesson_ReturnsLessonCancelledError()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();
        lesson.Cancel("Instructor is ill", _now);

        var result = lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.LessonCancelled);
    }

    [Fact]
    public void MarkAttendance_OtherGroup_ReturnsGroupMismatchError()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();
        var otherGroup = StudyGroup.Create("B-2026-03", _course, _instructorId, null, _start, _end, 20).Value;

        var result = lesson.MarkAttendance(otherGroup, contract, true, null, _instructorId, _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.GroupMismatch);
    }

    [Fact]
    public void MarkAttendance_ContractNotInGroup_ReturnsNotGroupMemberError()
    {
        var contract = EnrollStudent();
        _group.Withdraw(contract.Id, _now);
        var lesson = ScheduleLesson();

        var result = lesson.MarkAttendance(_group, contract, true, null, _instructorId, _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TheoryLesson.NotGroupMember);
        lesson.Attendance.Should().BeEmpty();
    }

    [Fact]
    public void MarkAttendance_CommentTooLong_ReturnsCommentTooLongErrorWithoutChanges()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();

        var result = lesson.MarkAttendance(_group, contract, true, new string('a', 501), _instructorId, _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LessonAttendance.CommentTooLong);
        contract.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public void MarkAttendance_EmptyMarkedByUserId_ReturnsMarkedByUserIdEmptyError()
    {
        var contract = EnrollStudent();
        var lesson = ScheduleLesson();

        var result = lesson.MarkAttendance(_group, contract, true, null, Guid.Empty, _duringLesson);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LessonAttendance.MarkedByUserIdEmpty);
    }
}
