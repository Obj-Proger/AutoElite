namespace AutoElite.Application.Contracts;

/// <summary>Loads and adds contracts for commands.</summary>
public interface IContractRepository : IRepository<Contract>
{
    /// <summary>Determines whether a contract already has the number, ignoring case.</summary>
    /// <param name="number">The trimmed number.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken = default);
}
