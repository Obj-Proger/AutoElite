using AutoElite.Application.TestSessions;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads test attempts with their questions in the order they were shown.</summary>
internal sealed class TestSessionRepository(AutoEliteDbContext dbContext)
    : Repository<TestSession>(dbContext), ITestSessionRepository
{
    /// <inheritdoc />
    public override Task<TestSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithQuestions().FirstOrDefaultAsync(session => session.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<TestSession?> GetInProgressOfContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        WithQuestions().FirstOrDefaultAsync(
            session => session.ContractId == contractId && session.Status == TestSessionStatus.InProgress,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsForExamAsync(Guid examId, Guid contractId, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(session => session.ExamId == examId && session.ContractId == contractId, cancellationToken);

    private IQueryable<TestSession> WithQuestions() =>
        Aggregates.Include(session => session.Questions.OrderBy(question => question.Position));
}
