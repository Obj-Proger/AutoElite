using AutoElite.Application.Practice;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakePracticeRecordRepository : InMemoryRepository<PracticeRecord>, IPracticeRecordRepository
{
    public Task<PracticeRecord?> FindAsync(Guid studentId, Guid questionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(record => record.StudentId == studentId && record.QuestionId == questionId));
}
