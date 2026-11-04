using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.TheoryLessons;

/// <summary>A lesson loaded for an instructor action, with its group.</summary>
/// <param name="Lesson">The lesson.</param>
/// <param name="Group">The group of the lesson.</param>
internal sealed record LessonWithGroup(TheoryLesson Lesson, StudyGroup Group);

/// <summary>
/// Loads a lesson for an action of its instructor. The instructor giving the lesson and the responsible instructor of
/// its group may act on it; for anyone else the lesson is reported as not found.
/// </summary>
internal sealed class TheoryLessonAccess(
    ITheoryLessonRepository lessonRepository,
    IStudyGroupRepository groupRepository,
    ICurrentUser currentUser)
{
    /// <summary>Loads the lesson and its group if the caller is one of their instructors.</summary>
    /// <param name="lessonId">The identifier of the lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<LessonWithGroup>> LoadAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        var group = lesson is null ? null : await groupRepository.GetByIdAsync(lesson.GroupId, cancellationToken);
        if (lesson is null || group is null)
            return Result.Failure<LessonWithGroup>(ApplicationErrors.NotFound<TheoryLesson>());

        var callerId = currentUser.GetRequiredUserId();

        return lesson.InstructorId == callerId || group.TheoryInstructorId == callerId
            ? new LessonWithGroup(lesson, group)
            : Result.Failure<LessonWithGroup>(ApplicationErrors.NotFound<TheoryLesson>());
    }
}
