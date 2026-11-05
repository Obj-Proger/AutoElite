using AutoElite.Application.Contracts;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds contracts.</summary>
internal sealed class ContractRepository(AutoEliteDbContext dbContext) : Repository<Contract>(dbContext), IContractRepository
{
    /// <inheritdoc />
    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(contract => contract.Number == number, cancellationToken);
}
