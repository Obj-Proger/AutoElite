using AutoElite.Application.Contracts;

namespace AutoElite.Application.Practice;

/// <summary>Finds the contract a student practises on, whose licence category chooses the questions.</summary>
internal sealed class PracticeContracts(IContractRepository contractRepository, ICurrentUser currentUser)
{
    /// <summary>Loads a contract of the signed-in student; the contracts of others are not found.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The contract, or not found.</returns>
    public async Task<Result<Contract>> LoadAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(contractId, cancellationToken);

        return contract is not null && contract.StudentId == currentUser.GetRequiredUserId()
            ? contract
            : Result.Failure<Contract>(ApplicationErrors.NotFound<Contract>());
    }
}
