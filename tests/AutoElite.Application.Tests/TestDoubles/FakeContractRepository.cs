using AutoElite.Application.Contracts;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeContractRepository : InMemoryRepository<Contract>, IContractRepository
{
    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(contract => string.Equals(contract.Number, number, StringComparison.OrdinalIgnoreCase)));
}
