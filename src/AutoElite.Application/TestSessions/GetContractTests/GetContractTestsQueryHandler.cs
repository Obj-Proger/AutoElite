namespace AutoElite.Application.TestSessions.GetContractTests;

/// <summary>Reads the history, hides contracts the caller may not see as not found and hides the results in progress.</summary>
internal sealed class GetContractTestsQueryHandler(ITestSessionQueries sessionQueries, ICurrentUser currentUser)
    : IQueryHandler<GetContractTestsQuery, IReadOnlyList<TestSessionItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TestSessionItem>>> Handle(GetContractTestsQuery query, CancellationToken cancellationToken)
    {
        var history = await sessionQueries.GetOfContractAsync(query.ContractId, cancellationToken);
        if (history is null || !TestResults.MayRead(currentUser, history.StudentId, history.BranchId))
            return Result.Failure<IReadOnlyList<TestSessionItem>>(ApplicationErrors.NotFound<Contract>());

        return Result.Success<IReadOnlyList<TestSessionItem>>([.. history.Sessions.Select(TestResults.Hide)]);
    }
}
