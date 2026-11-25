namespace AutoElite.Application.Practice.GetPracticeOverview;

/// <summary>Checks that the contract is the student's and counts the questions of its category.</summary>
internal sealed class GetPracticeOverviewQueryHandler(
    PracticeContracts contracts, IPracticeQueries practiceQueries, ICurrentUser currentUser)
    : IQueryHandler<GetPracticeOverviewQuery, PracticeOverview>
{
    /// <inheritdoc />
    public async Task<Result<PracticeOverview>> Handle(GetPracticeOverviewQuery query, CancellationToken cancellationToken)
    {
        var contract = await contracts.LoadAsync(query.ContractId, cancellationToken);
        if (contract.IsFailure)
            return Result.Failure<PracticeOverview>(contract.Error);

        return await practiceQueries.GetOverviewAsync(currentUser.GetRequiredUserId(), contract.Value.Category, cancellationToken);
    }
}
