namespace AutoElite.Application.Exams;

/// <summary>Reads exams for queries.</summary>
public interface IExamQueries
{
    /// <summary>Reads the exams that match the filter and whose group is inside the scope, the earliest first.</summary>
    /// <param name="filter">The conditions of the schedule.</param>
    /// <param name="scope">The branches the caller may see; groups without a branch are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<ExamItem>> GetScheduleAsync(
        ExamScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads the exams of a group.</summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The exams, or <see langword="null"/> if the group does not exist.</returns>
    Task<GroupExams?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the exams of a student, the latest first: the exams of the groups the student belongs to and every exam
    /// with a result of the student, each with the student's own result only.
    /// </summary>
    /// <param name="studentId">The identifier of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StudentExamItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of an exam.</summary>
    /// <param name="examId">The identifier of the exam.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the exam does not exist.</returns>
    Task<ExamDetails?> GetDetailsAsync(Guid examId, CancellationToken cancellationToken = default);
}
