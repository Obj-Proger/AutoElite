namespace AutoElite.Application.Analytics.GetLeadStatistics;

/// <summary>Resolves what the report covers and reads it.</summary>
internal sealed class GetLeadStatisticsQueryHandler(ReportScopes reportScopes, IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetLeadStatisticsQuery, LeadStatistics>
{
    /// <inheritdoc />
    public async Task<Result<LeadStatistics>> Handle(GetLeadStatisticsQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<LeadStatistics>(scopeResult.Error);

        return Result.Success(await analyticsQueries.GetLeadStatisticsAsync(scopeResult.Value, cancellationToken));
    }
}
