namespace AutoElite.Application.Analytics.GetLeadStatistics;

/// <summary>Counts the leads that arrived in the period by source and by current status, with the conversion (FR-2.7, FR-14.2).</summary>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetLeadStatisticsQuery(DateOnly From, DateOnly To, Guid? BranchId = null) : IQuery<LeadStatistics>, IReportQuery;
