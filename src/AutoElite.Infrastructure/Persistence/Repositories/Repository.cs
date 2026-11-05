namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds aggregates of one type; loaded aggregates are tracked, so the unit of work saves their changes.</summary>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
internal abstract class Repository<TAggregate>(AutoEliteDbContext dbContext) : IRepository<TAggregate>
    where TAggregate : BaseEntity
{
    /// <summary>Gets the aggregates, tracked.</summary>
    protected DbSet<TAggregate> Aggregates { get; } = dbContext.Set<TAggregate>();

    /// <summary>Gets the context, for repositories that need more than their own table.</summary>
    protected AutoEliteDbContext DbContext { get; } = dbContext;

    /// <inheritdoc />
    /// <remarks>Aggregates with child entities override it to load them.</remarks>
    public virtual Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Aggregates.FirstOrDefaultAsync(aggregate => aggregate.Id == id, cancellationToken);

    /// <inheritdoc />
    public virtual void Add(TAggregate aggregate) => Aggregates.Add(aggregate);
}
