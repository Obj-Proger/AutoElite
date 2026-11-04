namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Commits the changes a command made to its aggregates as a single transaction. Repositories only track changes;
/// nothing reaches the database until <see cref="SaveChangesAsync"/> is called.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Saves every tracked change in one transaction.</summary>
    /// <remarks>
    /// Command handlers do not call it: the pipeline saves once after a command succeeds and never after a failure.
    /// </remarks>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <exception cref="ConcurrencyConflictException">A record was changed by another request after it was loaded.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
