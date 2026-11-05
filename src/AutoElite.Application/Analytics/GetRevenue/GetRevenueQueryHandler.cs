namespace AutoElite.Application.Analytics.GetRevenue;

/// <summary>Resolves what the report covers and reads it.</summary>
internal sealed class GetRevenueQueryHandler(ReportScopes reportScopes, IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetRevenueQuery, IReadOnlyList<MonthlyRevenue>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MonthlyRevenue>>> Handle(GetRevenueQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<IReadOnlyList<MonthlyRevenue>>(scopeResult.Error);

        return Result.Success(await analyticsQueries.GetRevenueAsync(scopeResult.Value, cancellationToken));
    }
}
