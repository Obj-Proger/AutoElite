namespace AutoElite.Application.Analytics.GetStaffActivity;

/// <summary>Resolves what the report covers and reads it.</summary>
internal sealed class GetStaffActivityQueryHandler(ReportScopes reportScopes, IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetStaffActivityQuery, IReadOnlyList<StaffActivityItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StaffActivityItem>>> Handle(GetStaffActivityQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<IReadOnlyList<StaffActivityItem>>(scopeResult.Error);

        return Result.Success(await analyticsQueries.GetStaffActivityAsync(scopeResult.Value, cancellationToken));
    }
}
