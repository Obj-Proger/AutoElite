using AutoElite.Application.TestSessions;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeTestSessionRepository : InMemoryRepository<TestSession>, ITestSessionRepository
{
    public Task<TestSession?> GetInProgressOfContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(session =>
            session.ContractId == contractId && session.Status == TestSessionStatus.InProgress));
}
