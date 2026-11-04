namespace AutoElite.Application.TestSessions;

/// <summary>Loads and adds test sessions for commands; a loaded session includes its questions.</summary>
public interface ITestSessionRepository : IRepository<TestSession>
{
    /// <summary>Loads the attempt of the contract that is still in progress, if any, even if its time has run out.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<TestSession?> GetInProgressOfContractAsync(Guid contractId, CancellationToken cancellationToken = default);
}
