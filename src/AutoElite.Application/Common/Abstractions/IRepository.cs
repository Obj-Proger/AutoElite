namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Loads and adds aggregates of one type for commands. Loaded aggregates are tracked, so their changes are saved
/// by <see cref="IUnitOfWork"/> without an explicit update call. Queries read through dedicated read services instead.
/// </summary>
/// <remarks>
/// Aggregates that need more than lookup by identifier get their own interface derived from this one,
/// for example a lookup of a user by email.
/// </remarks>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
public interface IRepository<TAggregate>
    where TAggregate : BaseEntity
{
    /// <summary>Loads the aggregate with all the child entities its rules need.</summary>
    /// <param name="id">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The aggregate, or <see langword="null"/> if it does not exist.</returns>
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Starts tracking a new aggregate; it is inserted on the next save.</summary>
    /// <param name="aggregate">The new aggregate.</param>
    void Add(TAggregate aggregate);
}
