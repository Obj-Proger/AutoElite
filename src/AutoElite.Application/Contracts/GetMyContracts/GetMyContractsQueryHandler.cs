namespace AutoElite.Application.Contracts.GetMyContracts;

/// <summary>Reads the caller's contracts.</summary>
internal sealed class GetMyContractsQueryHandler(IContractQueries contractQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyContractsQuery, IReadOnlyList<ContractListItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ContractListItem>>> Handle(GetMyContractsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await contractQueries.GetOfStudentAsync(currentUser.GetRequiredUserId(), cancellationToken));
}
