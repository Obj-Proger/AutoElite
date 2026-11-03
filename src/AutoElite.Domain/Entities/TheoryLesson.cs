using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A theory lesson of a <see cref="StudyGroup"/> on one topic of the course, with the attendance of its students.
/// </summary>
/// <remarks>
/// <para>
/// The lesson becomes <see cref="TheoryLessonStatus.Completed"/> on its own once the attendance of every current
/// member of the group has been marked. Marks can still be corrected afterwards.
/// </para>
/// <para>
/// Marking a student present counts the lesson on the student's contract, and correcting the mark to absent takes
/// it back, in the same call; the caller saves the lesson and the contract in one unit of work. Learning materials
/// are attached as documents owned by the lesson or by the course topic.
/// </para>
/// </remarks>
public sealed class TheoryLesson : AuditableEntity
{
    /// <summary>The longest lesson that can be scheduled.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);

    private const int MaxCancellationReasonLength = 1000;

    private readonly List<LessonAttendance> _attendance = [];

    /// <summary>Gets the identifier of the study group.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the identifier of the course topic the lesson covers.</summary>
    public Guid TopicId { get; private set; }

    /// <summary>Gets the identifier of the theory instructor giving the lesson.</summary>
    public Guid InstructorId { get; private set; }

    /// <summary>Gets when the lesson takes place.</summary>
    public TimeInterval Time { get; private set; } = null!;

    /// <summary>Gets the lesson status.</summary>
    public TheoryLessonStatus Status { get; private set; }

    /// <summary>Gets the reason the lesson was cancelled, or <see langword="null"/> if it was not.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>Gets the attendance marks, one per student, in the order they were first made.</summary>
    public IReadOnlyList<LessonAttendance> Attendance => _attendance.AsReadOnly();

    private TheoryLesson(Guid id, StudyGroup group, Guid topicId, Guid instructorId, TimeInterval time) : base(id)
    {
        GroupId = group.Id;
        TopicId = topicId;
        InstructorId = instructorId;
        Time = time;
        Status = TheoryLessonStatus.Scheduled;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private TheoryLesson()
    {
    }

    /// <summary>Schedules a lesson for a group.</summary>
    /// <param name="group">The study group; it must be active.</param>
    /// <param name="course">The group's course, used to check the topic.</param>
    /// <param name="topicId">A topic of the course.</param>
    /// <param name="instructorId">The theory instructor giving the lesson; checking the role is up to the caller.</param>
    /// <param name="time">When the lesson takes place; it must start in the future and last at most <see cref="MaxDuration"/>.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The scheduled lesson, or the first validation failure encountered.</returns>
    public static Result<TheoryLesson> Schedule(
        StudyGroup group, Course course, Guid topicId, Guid instructorId, TimeInterval time, DateTime utcNow)
    {
        if (!group.IsActive)
            return Result.Failure<TheoryLesson>(DomainErrors.TheoryLesson.GroupInactive);

        if (course.Id != group.CourseId)
            return Result.Failure<TheoryLesson>(DomainErrors.TheoryLesson.CourseMismatch);

        if (!course.Topics.Any(topic => topic.Id == topicId))
            return Result.Failure<TheoryLesson>(DomainErrors.TheoryLesson.TopicNotInCourse);

        var instructorResult = Guard.NotEmpty(instructorId, DomainErrors.TheoryLesson.InstructorIdEmpty);
        if (instructorResult.IsFailure)
            return Result.Failure<TheoryLesson>(instructorResult.Error);

        var timeResult = ValidateTime(time, utcNow);
        if (timeResult.IsFailure)
            return Result.Failure<TheoryLesson>(timeResult.Error);

        return new TheoryLesson(Guid.CreateVersion7(), group, topicId, instructorId, time);
    }

    /// <summary>Moves a lesson that has no attendance yet and raises <see cref="TheoryLessonRescheduledDomainEvent"/>.</summary>
    /// <param name="time">The new time; it must start in the future and last at most <see cref="MaxDuration"/>.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the lesson is not scheduled, attendance has been marked, or the time is invalid.</returns>
    public Result Reschedule(TimeInterval time, DateTime utcNow)
    {
        if (Status != TheoryLessonStatus.Scheduled)
            return Result.Failure(DomainErrors.TheoryLesson.NotScheduled);

        if (_attendance.Count > 0)
            return Result.Failure(DomainErrors.TheoryLesson.AttendanceAlreadyMarked);

        var timeResult = ValidateTime(time, utcNow);
        if (timeResult.IsFailure)
            return timeResult;

        Time = time;
        RaiseDomainEvent(new TheoryLessonRescheduledDomainEvent(Id, GroupId, time.StartUtc, time.EndUtc, utcNow));

        return Result.Success();
    }

    /// <summary>Changes the instructor giving the lesson, for example a substitute.</summary>
    /// <returns>A failure if the lesson is not scheduled or the identifier is empty.</returns>
    public Result ChangeInstructor(Guid instructorId)
    {
        if (Status != TheoryLessonStatus.Scheduled)
            return Result.Failure(DomainErrors.TheoryLesson.NotScheduled);

        var instructorResult = Guard.NotEmpty(instructorId, DomainErrors.TheoryLesson.InstructorIdEmpty);
        if (instructorResult.IsFailure)
            return Result.Failure(instructorResult.Error);

        InstructorId = instructorId;

        return Result.Success();
    }

    /// <summary>Cancels a scheduled lesson and raises <see cref="TheoryLessonCancelledDomainEvent"/>, so that the group is notified.</summary>
    /// <param name="reason">Why the lesson is cancelled. Required.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the lesson is not scheduled, attendance has been marked, or the reason is invalid.</returns>
    public Result Cancel(string? reason, DateTime utcNow)
    {
        if (Status != TheoryLessonStatus.Scheduled)
            return Result.Failure(DomainErrors.TheoryLesson.NotScheduled);

        if (_attendance.Count > 0)
            return Result.Failure(DomainErrors.TheoryLesson.AttendanceAlreadyMarked);

        var reasonResult = Guard.RequiredText(
            reason, MaxCancellationReasonLength, DomainErrors.TheoryLesson.CancellationReasonEmpty, DomainErrors.TheoryLesson.CancellationReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        Status = TheoryLessonStatus.Cancelled;
        CancellationReason = reasonResult.Value;
        RaiseDomainEvent(new TheoryLessonCancelledDomainEvent(Id, GroupId, Time.StartUtc, reasonResult.Value, utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Marks or corrects the attendance of one student and updates the theory counter of the student's contract.
    /// Completes the lesson once every current member of the group has a mark.
    /// </summary>
    /// <param name="group">The lesson's group, used to check membership and completion.</param>
    /// <param name="contract">The student's contract; it must be a current member of the group.</param>
    /// <param name="isPresent">Whether the student attended.</param>
    /// <param name="comment">An optional comment, for example the reason for absence.</param>
    /// <param name="markedByUserId">The instructor making the mark.</param>
    /// <param name="utcNow">The current UTC time; the lesson must have started.</param>
    /// <returns>
    /// A failure if the lesson is cancelled or has not started, the group or contract does not match, an argument is
    /// invalid, or the contract rejects the change.
    /// </returns>
    public Result MarkAttendance(
        StudyGroup group, Contract contract, bool isPresent, string? comment, Guid markedByUserId, DateTime utcNow)
    {
        if (Status == TheoryLessonStatus.Cancelled)
            return Result.Failure(DomainErrors.TheoryLesson.LessonCancelled);

        if (utcNow < Time.StartUtc)
            return Result.Failure(DomainErrors.TheoryLesson.NotStarted);

        if (group.Id != GroupId)
            return Result.Failure(DomainErrors.TheoryLesson.GroupMismatch);

        if (!group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.ContractId == contract.Id))
            return Result.Failure(DomainErrors.TheoryLesson.NotGroupMember);

        var markResult = LessonAttendance.Validate(comment, markedByUserId);
        if (markResult.IsFailure)
            return markResult;

        var existing = _attendance.Find(mark => mark.ContractId == contract.Id);
        var wasPresent = existing?.IsPresent ?? false;

        if (isPresent != wasPresent)
        {
            var counterResult = isPresent ? contract.RecordTheoryAttendance() : contract.RevokeTheoryAttendance();
            if (counterResult.IsFailure)
                return counterResult;
        }

        if (existing is null)
            _attendance.Add(LessonAttendance.Create(contract.Id, contract.StudentId, isPresent, comment, markedByUserId, utcNow));
        else
            existing.Correct(isPresent, comment, markedByUserId, utcNow);

        if (Status == TheoryLessonStatus.Scheduled && EveryCurrentMemberIsMarked(group))
            Status = TheoryLessonStatus.Completed;

        return Result.Success();
    }

    private bool EveryCurrentMemberIsMarked(StudyGroup group) =>
        group.Enrollments
            .Where(enrollment => enrollment.IsCurrent)
            .All(enrollment => _attendance.Exists(mark => mark.ContractId == enrollment.ContractId));

    private static Result ValidateTime(TimeInterval time, DateTime utcNow)
    {
        if (time.StartUtc <= utcNow)
            return Result.Failure(DomainErrors.TheoryLesson.StartInPast);

        if (time.Duration > MaxDuration)
            return Result.Failure(DomainErrors.TheoryLesson.DurationTooLong);

        return Result.Success();
    }
}
