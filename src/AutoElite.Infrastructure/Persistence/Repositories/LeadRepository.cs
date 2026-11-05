namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads leads with their notes and status history, which the rules of the lead need.</summary>
internal sealed class LeadRepository(AutoEliteDbContext dbContext) : Repository<Lead>(dbContext)
{
    /// <inheritdoc />
    /// <remarks>The two collections are read by separate queries, so that their rows do not multiply.</remarks>
    public override Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Aggregates
            .Include(lead => lead.Notes)
            .Include(lead => lead.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);
}
