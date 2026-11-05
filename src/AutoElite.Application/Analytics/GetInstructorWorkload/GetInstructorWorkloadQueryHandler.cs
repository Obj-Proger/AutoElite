namespace AutoElite.Application.Analytics.GetInstructorWorkload;

/// <summary>Resolves what the report covers and reads it.</summary>
internal sealed class GetInstructorWorkloadQueryHandler(ReportScopes reportScopes, IAnalyticsQueries analyticsQueries)
    : IQueryHandler<GetInstructorWorkloadQuery, IReadOnlyList<InstructorWorkloadItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InstructorWorkloadItem>>> Handle(
        GetInstructorWorkloadQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<IReadOnlyList<InstructorWorkloadItem>>(scopeResult.Error);

        return Result.Success(await analyticsQueries.GetInstructorWorkloadAsync(scopeResult.Value, cancellationToken));
    }
}
