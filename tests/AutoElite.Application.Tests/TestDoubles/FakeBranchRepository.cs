using AutoElite.Application.Branches;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeBranchRepository : InMemoryRepository<Branch>, IBranchRepository
{
    public Task<bool> NameExistsAsync(string name, Guid? exceptBranchId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(branch =>
            branch.Id != exceptBranchId && string.Equals(branch.Name, name, StringComparison.OrdinalIgnoreCase)));
}
