namespace AutoElite.Application.Analytics.GetContractSummary;

/// <summary>
/// Counts the contracts drawn up in the period by their current status, with the share of successful completion and
/// the average quality score (FR-14.3).
/// </summary>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
/// <param name="CourseId">Only contracts for this course, if given.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetContractSummaryQuery(DateOnly From, DateOnly To, Guid? BranchId = null, Guid? CourseId = null)
    : IQuery<ContractSummary>, IReportQuery;
