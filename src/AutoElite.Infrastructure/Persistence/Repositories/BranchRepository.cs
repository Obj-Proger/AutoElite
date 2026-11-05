using AutoElite.Application.Branches;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds branches; the name column ignores case, so the lookup does too.</summary>
internal sealed class BranchRepository(AutoEliteDbContext dbContext) : Repository<Branch>(dbContext), IBranchRepository
{
    /// <inheritdoc />
    public Task<bool> NameExistsAsync(string name, Guid? exceptBranchId = null, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(branch => branch.Name == name && branch.Id != exceptBranchId, cancellationToken);
}
