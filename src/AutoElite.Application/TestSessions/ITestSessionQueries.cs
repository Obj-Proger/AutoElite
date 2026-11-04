namespace AutoElite.Application.TestSessions;

/// <summary>
/// Reads test attempts for queries. The results are returned in full; the handlers hide them while an attempt is in
/// progress.
/// </summary>
public interface ITestSessionQueries
{
    /// <summary>Reads the attempts of a contract.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The attempts, or <see langword="null"/> if the contract does not exist.</returns>
    Task<ContractTestSessions?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of an attempt.</summary>
    /// <param name="sessionId">The identifier of the attempt.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the attempt does not exist.</returns>
    Task<TestSessionDetails?> GetDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
