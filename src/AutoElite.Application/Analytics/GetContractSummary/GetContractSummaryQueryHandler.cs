namespace AutoElite.Application.Analytics.GetContractSummary;

/// <summary>Resolves what the report covers and reads it.</summary>
internal sealed class GetContractSummaryQueryHandler(ReportScopes reportScopes, IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetContractSummaryQuery, ContractSummary>
{
    /// <inheritdoc />
    public async Task<Result<ContractSummary>> Handle(GetContractSummaryQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<ContractSummary>(scopeResult.Error);

        return await analyticsQueries.GetContractSummaryAsync(scopeResult.Value, query.CourseId, cancellationToken);
    }
}
