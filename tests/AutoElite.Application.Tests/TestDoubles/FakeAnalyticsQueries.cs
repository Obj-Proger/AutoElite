using AutoElite.Application.Analytics;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns empty reports and records what was asked; the counting itself is the infrastructure's job.</summary>
internal sealed class FakeAnalyticsQueries : IAnalyticsQueries
{
    public ReportScope? LastScope { get; private set; }

    public Guid? LastCourseId { get; private set; }

    public Task<LeadStatistics> GetLeadStatisticsAsync(ReportScope scope, CancellationToken cancellationToken = default)
    {
        LastScope = scope;

        return Task.FromResult(new LeadStatistics(0, 0, [], []));
    }

    public Task<IReadOnlyList<MonthlyRevenue>> GetRevenueAsync(ReportScope scope, CancellationToken cancellationToken = default)
    {
        LastScope = scope;

        return Task.FromResult<IReadOnlyList<MonthlyRevenue>>([]);
    }

    public Task<ContractSummary> GetContractSummaryAsync(
        ReportScope scope, Guid? courseId, CancellationToken cancellationToken = default)
    {
        LastScope = scope;
        LastCourseId = courseId;

        return Task.FromResult(new ContractSummary(0, 0, 0, null));
    }

    public Task<IReadOnlyList<InstructorWorkloadItem>> GetInstructorWorkloadAsync(
        ReportScope scope, CancellationToken cancellationToken = default)
    {
        LastScope = scope;

        return Task.FromResult<IReadOnlyList<InstructorWorkloadItem>>([]);
    }

    public Task<IReadOnlyList<StaffActivityItem>> GetStaffActivityAsync(
        ReportScope scope, CancellationToken cancellationToken = default)
    {
        LastScope = scope;

        return Task.FromResult<IReadOnlyList<StaffActivityItem>>([]);
    }
}
