using AutoElite.Application.Exams;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeExamRepository : InMemoryRepository<Exam>, IExamRepository
{
    public Task<IReadOnlyList<Exam>> GetScheduledBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Exam>>([
            .. Items.Where(exam => exam.Status == ExamStatus.Scheduled && exam.ScheduledAtUtc > afterUtc && exam.ScheduledAtUtc <= untilUtc)
        ]);
}
