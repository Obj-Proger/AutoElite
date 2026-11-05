namespace AutoElite.Application.Analytics;

/// <summary>
/// Resolves what a report covers. A manager tied to a branch reports on that branch only; administrators and managers
/// of the whole school report on the whole school or on any branch.
/// </summary>
internal sealed class ReportScopes(IRepository<Branch> branchRepository, LocalTimes localTimes, ICurrentUser currentUser)
{
    /// <summary>Resolves the branch, the time zone and the period of a report query.</summary>
    /// <param name="query">The report query; its period and branch are already validated.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The scope, Forbidden for a branch outside the caller's scope, or not found for an unknown branch.</returns>
    public async Task<Result<ReportScope>> ResolveAsync(IReportQuery query, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);
        if (query.BranchId is { } requested)
        {
            if (!scope.Includes(requested))
                return Result.Failure<ReportScope>(ApplicationErrors.Auth.Forbidden);

            if (await branchRepository.GetByIdAsync(requested, cancellationToken) is null)
                return Result.Failure<ReportScope>(ApplicationErrors.NotFound<Branch>());
        }

        var branchId = query.BranchId ?? scope.BranchId;
        var timeZone = await localTimes.GetTimeZoneAsync(branchId, cancellationToken);
        var periodResult = TimeInterval.Create(StartOf(query.From, timeZone), StartOf(query.To.AddDays(1), timeZone));
        if (periodResult.IsFailure)
            return Result.Failure<ReportScope>(periodResult.Error);

        return new ReportScope(branchId, periodResult.Value, timeZone);
    }

    /// <summary>
    /// Finds the UTC instant a local day begins. Where clocks move forward at midnight, the day begins at the first
    /// local time that exists.
    /// </summary>
    private static DateTime StartOf(DateOnly day, TimeZoneInfo timeZone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
            local = local.AddMinutes(15);

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
