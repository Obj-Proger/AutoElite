using AutoElite.Application.Exams;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads exams with their results in the order they were recorded; identifiers of version 7 sort by time.</summary>
internal sealed class ExamRepository(AutoEliteDbContext dbContext) : Repository<Exam>(dbContext), IExamRepository
{
    /// <inheritdoc />
    public override Task<Exam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Aggregates
            .Include(exam => exam.Results.OrderBy(result => result.Id))
            .FirstOrDefaultAsync(exam => exam.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Exam>> GetScheduledBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        await Aggregates
            .Include(exam => exam.Results.OrderBy(result => result.Id))
            .Where(exam => exam.Status == ExamStatus.Scheduled && exam.ScheduledAtUtc > afterUtc && exam.ScheduledAtUtc <= untilUtc)
            .ToListAsync(cancellationToken);
}
