namespace AutoElite.Application.Analytics.GetRevenue;

/// <summary>Sums the money received and refunded in each month of the period, separately for each currency (FR-14.1).</summary>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetRevenueQuery(DateOnly From, DateOnly To, Guid? BranchId = null)
    : IQuery<IReadOnlyList<MonthlyRevenue>>, IReportQuery;
