namespace AutoElite.Application.Analytics.GetInstructorWorkload;

/// <summary>
/// Shows the work of each instructor on lessons that started in the period: lessons, hours, cancellations, no-shows
/// and the average rating (FR-14.4).
/// </summary>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetInstructorWorkloadQuery(DateOnly From, DateOnly To, Guid? BranchId = null)
    : IQuery<IReadOnlyList<InstructorWorkloadItem>>, IReportQuery;
