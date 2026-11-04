namespace AutoElite.Application.TestSessions.GetTestSession;

/// <summary>Reads the card, hides attempts the caller may not see as not found and hides the results in progress.</summary>
internal sealed class GetTestSessionQueryHandler(ITestSessionQueries sessionQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTestSessionQuery, TestSessionDetails>
{
    /// <inheritdoc />
    public async Task<Result<TestSessionDetails>> Handle(GetTestSessionQuery query, CancellationToken cancellationToken)
    {
        var details = await sessionQueries.GetDetailsAsync(query.SessionId, cancellationToken);
        if (details is null || !TestResults.MayRead(currentUser, details.StudentId, details.BranchId))
            return Result.Failure<TestSessionDetails>(ApplicationErrors.NotFound<TestSession>());

        return TestResults.Hide(details);
    }
}
