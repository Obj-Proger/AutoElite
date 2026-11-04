using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;

/// <summary>
/// Checks that the caller is the responsible instructor of the group, checks a substitute and the time, and
/// schedules the lesson.
/// </summary>
internal sealed class ScheduleTheoryLessonCommandHandler(
    ITheoryLessonRepository lessonRepository,
    IStudyGroupRepository groupRepository,
    IRepository<Course> courseRepository,
    LessonInstructors lessonInstructors,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ScheduleTheoryLessonCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(ScheduleTheoryLessonCommand command, CancellationToken cancellationToken)
    {
        var callerId = currentUser.GetRequiredUserId();
        var group = await groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group is null || group.TheoryInstructorId != callerId)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<StudyGroup>());

        var course = await courseRepository.GetByIdAsync(group.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Course>());

        var instructorId = command.InstructorId ?? callerId;
        if (instructorId != callerId)
        {
            var instructorResult = await lessonInstructors.CheckAsync(instructorId, group, cancellationToken);
            if (instructorResult.IsFailure)
                return Result.Failure<Guid>(instructorResult.Error);
        }

        var timeResult = await LessonTime.CheckAsync(
            lessonRepository, group.Id, instructorId, command.StartUtc, command.EndUtc, null, cancellationToken);
        if (timeResult.IsFailure)
            return Result.Failure<Guid>(timeResult.Error);

        var lessonResult = TheoryLesson.Schedule(
            group, course, command.TopicId, instructorId, timeResult.Value, timeProvider.GetUtcNow().UtcDateTime);
        if (lessonResult.IsFailure)
            return Result.Failure<Guid>(lessonResult.Error);

        lessonRepository.Add(lessonResult.Value);

        return lessonResult.Value.Id;
    }
}
