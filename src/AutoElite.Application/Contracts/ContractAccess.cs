namespace AutoElite.Application.Contracts;

/// <summary>Loads a contract for a staff action, hiding contracts outside the caller's branch scope as not found.</summary>
internal sealed class ContractAccess(IContractRepository contractRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the contract if it is inside the caller's scope.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Contract>> LoadAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(contractId, cancellationToken);

        return contract is not null && DataScope.For(currentUser).Includes(contract.BranchId)
            ? contract
            : Result.Failure<Contract>(ApplicationErrors.NotFound<Contract>());
    }
}
