namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Commits the changes a command made to its aggregates as a single transaction. Repositories only track changes;
/// nothing reaches the database until <see cref="SaveChangesAsync"/> is called.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Saves every tracked change in one transaction.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
