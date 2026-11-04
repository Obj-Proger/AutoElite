namespace AutoElite.Application.TheoryLessons;

/// <summary>Reads theory lessons for queries.</summary>
public interface ITheoryLessonQueries
{
    /// <summary>Reads the lessons that match the filter and whose groups are inside the scope, the earliest first.</summary>
    /// <param name="filter">The conditions of the schedule.</param>
    /// <param name="scope">The branches the caller may see; groups without a branch are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<TheoryLessonItem>> GetScheduleAsync(
        TheoryScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the lessons of a student that overlap the period, the earliest first: the lessons of each group the
    /// student belongs to, from the enrollment to the withdrawal, and every lesson the student was marked at.
    /// </summary>
    /// <param name="studentId">The identifier of the student.</param>
    /// <param name="period">The time to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StudentTheoryLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default);

    /// <summary>Reads all lessons of a group.</summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lessons, or <see langword="null"/> if the group does not exist.</returns>
    Task<GroupTheoryLessons?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a lesson.</summary>
    /// <param name="lessonId">The identifier of the lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the lesson does not exist.</returns>
    Task<TheoryLessonDetails?> GetDetailsAsync(Guid lessonId, CancellationToken cancellationToken = default);
}
