using System.Linq.Expressions;
using AutoElite.Application.Branches;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads branches for queries, without tracking.</summary>
internal sealed class BranchQueries(AutoEliteDbContext dbContext) : IBranchQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BranchItem>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var branches = dbContext.Set<Branch>().AsNoTracking();
        if (isActive is { } active)
            branches = branches.Where(branch => branch.IsActive == active);

        return await branches.OrderBy(branch => branch.Name).Select(Item()).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<BranchItem?> GetAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Branch>().AsNoTracking()
            .Where(branch => branch.Id == branchId)
            .Select(Item())
            .FirstOrDefaultAsync(cancellationToken);

    private static Expression<Func<Branch, BranchItem>> Item() =>
        branch => new BranchItem(
            branch.Id,
            branch.Name,
            branch.Address.City,
            branch.Address.Street,
            ValueOf(branch.Phone),
            branch.TimeZoneId,
            branch.IsActive);

    // Evaluated after the row is read: the phone column is converted to a value object, whose members SQL cannot reach.
    private static string? ValueOf(PhoneNumber? phone) => phone?.Value;
}
