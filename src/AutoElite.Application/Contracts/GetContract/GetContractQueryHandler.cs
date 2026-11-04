namespace AutoElite.Application.Contracts.GetContract;

/// <summary>Reads the card and hides contracts the caller may not see as not found.</summary>
internal sealed class GetContractQueryHandler(IContractQueries contractQueries, ICurrentUser currentUser)
    : IQueryHandler<GetContractQuery, ContractDetails>
{
    /// <inheritdoc />
    public async Task<Result<ContractDetails>> Handle(GetContractQuery query, CancellationToken cancellationToken)
    {
        var details = await contractQueries.GetDetailsAsync(query.ContractId, cancellationToken);

        return details is not null && MayRead(details)
            ? details
            : Result.Failure<ContractDetails>(ApplicationErrors.NotFound<Contract>());
    }

    private bool MayRead(ContractDetails details)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(details.BranchId))
            return true;

        return details.StudentId == currentUser.UserId;
    }
}
