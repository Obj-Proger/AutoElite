namespace AutoElite.Application.TheoryLessons;

/// <summary>Loads and adds theory lessons for commands; a loaded lesson includes its attendance.</summary>
public interface ITheoryLessonRepository : IRepository<TheoryLesson>
{
    /// <summary>
    /// Determines whether another lesson that is not cancelled overlaps the time for the group or for the instructor,
    /// since neither can be in two lessons at once.
    /// </summary>
    /// <param name="groupId">The group of the lesson.</param>
    /// <param name="instructorId">The instructor of the lesson.</param>
    /// <param name="time">The time of the lesson.</param>
    /// <param name="exceptLessonId">A lesson to leave out, when that lesson is being moved.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> HasOverlapAsync(
        Guid groupId, Guid instructorId, TimeInterval time, Guid? exceptLessonId = null, CancellationToken cancellationToken = default);
}
