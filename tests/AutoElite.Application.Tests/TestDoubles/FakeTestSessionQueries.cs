using AutoElite.Application.TestSessions;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded histories and cards.</summary>
internal sealed class FakeTestSessionQueries : ITestSessionQueries
{
    private readonly Dictionary<Guid, ContractTestSessions> _contracts = [];
    private readonly Dictionary<Guid, TestSessionDetails> _details = [];

    public void Seed(Guid contractId, ContractTestSessions sessions) => _contracts.Add(contractId, sessions);

    public void Seed(TestSessionDetails details) => _details.Add(details.Session.Id, details);

    public Task<ContractTestSessions?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_contracts.GetValueOrDefault(contractId));

    public Task<TestSessionDetails?> GetDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(sessionId));
}
