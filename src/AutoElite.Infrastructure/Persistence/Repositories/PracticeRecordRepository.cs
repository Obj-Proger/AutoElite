using AutoElite.Application.Practice;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds the practice records of the students.</summary>
internal sealed class PracticeRecordRepository(AutoEliteDbContext dbContext)
    : Repository<PracticeRecord>(dbContext), IPracticeRecordRepository
{
    /// <inheritdoc />
    public Task<PracticeRecord?> FindAsync(Guid studentId, Guid questionId, CancellationToken cancellationToken = default) =>
        Aggregates.FirstOrDefaultAsync(
            record => record.StudentId == studentId && record.QuestionId == questionId, cancellationToken);
}
