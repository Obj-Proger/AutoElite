namespace AutoElite.Application.Analytics.GetStaffActivity;

/// <summary>Counts the tasks set, completed and overdue of each staff member in the period (FR-14.5).</summary>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetStaffActivityQuery(DateOnly From, DateOnly To, Guid? BranchId = null)
    : IQuery<IReadOnlyList<StaffActivityItem>>, IReportQuery;
