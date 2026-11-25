namespace AutoElite.Application.Practice;

/// <summary>Loads and adds the practice records of the students for commands.</summary>
public interface IPracticeRecordRepository : IRepository<PracticeRecord>
{
    /// <summary>Loads the record of a student for a question, tracked.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The record, or <see langword="null"/> if the student has not answered the question in practice.</returns>
    Task<PracticeRecord?> FindAsync(Guid studentId, Guid questionId, CancellationToken cancellationToken = default);
}
