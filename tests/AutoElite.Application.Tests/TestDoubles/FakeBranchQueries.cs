using AutoElite.Application.Branches;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeBranchQueries : IBranchQueries
{
    private readonly List<BranchItem> _branches = [];

    public void Seed(BranchItem branch) => _branches.Add(branch);

    public Task<IReadOnlyList<BranchItem>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BranchItem>>(
            [.. _branches.Where(branch => isActive is null || branch.IsActive == isActive).OrderBy(branch => branch.Name)]);

    public Task<BranchItem?> GetAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_branches.SingleOrDefault(branch => branch.Id == branchId));
}
