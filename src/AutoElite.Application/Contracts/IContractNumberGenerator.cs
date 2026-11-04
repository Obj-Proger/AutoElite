namespace AutoElite.Application.Contracts;

/// <summary>
/// Gives out contract numbers for contracts drawn up without one. The infrastructure takes them from a database
/// sequence, so concurrent requests never get the same number.
/// </summary>
public interface IContractNumberGenerator
{
    /// <summary>Gives out the next number, for example <c>2026-000123</c> for a contract starting in 2026.</summary>
    /// <param name="startDate">The first day of training, whose year starts the number.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<string> NextAsync(DateOnly startDate, CancellationToken cancellationToken = default);
}
