namespace AutoElite.Application.Analytics;

/// <summary>Reads the reports of the analytics module (M-14).</summary>
/// <remarks>Each report covers <see cref="ReportScope.Period"/> and, if given, <see cref="ReportScope.BranchId"/>.</remarks>
public interface IAnalyticsQueries
{
    /// <summary>Counts the leads that arrived in the period, by source and by current status.</summary>
    /// <param name="scope">What the report covers.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<LeadStatistics> GetLeadStatisticsAsync(ReportScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums the payments confirmed and refunded in each month of the period, months counted in
    /// <see cref="ReportScope.TimeZone"/>, separately for each currency; months without money are left out.
    /// </summary>
    /// <param name="scope">What the report covers; a payment belongs to the branch of its contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<MonthlyRevenue>> GetRevenueAsync(ReportScope scope, CancellationToken cancellationToken = default);

    /// <summary>Counts the contracts drawn up in the period by their current status.</summary>
    /// <param name="scope">What the report covers.</param>
    /// <param name="courseId">Only contracts for this course, if given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<ContractSummary> GetContractSummaryAsync(ReportScope scope, Guid? courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums the lessons of each instructor that started in the period; an instructor belongs to their branch, and
    /// instructors without lessons are left out.
    /// </summary>
    /// <param name="scope">What the report covers.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<InstructorWorkloadItem>> GetInstructorWorkloadAsync(
        ReportScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the tasks of each staff member in the period; a staff member belongs to their branch, and staff without
    /// tasks are left out.
    /// </summary>
    /// <param name="scope">What the report covers.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StaffActivityItem>> GetStaffActivityAsync(ReportScope scope, CancellationToken cancellationToken = default);
}
