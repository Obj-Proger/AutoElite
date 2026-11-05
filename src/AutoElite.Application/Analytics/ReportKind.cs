namespace AutoElite.Application.Analytics;

/// <summary>The reports that can be exported.</summary>
public enum ReportKind
{
    /// <summary>Leads and conversion (FR-2.7, FR-14.2).</summary>
    LeadStatistics = 1,

    /// <summary>Revenue by month (FR-14.1).</summary>
    Revenue = 2,

    /// <summary>The contract summary (FR-14.3).</summary>
    ContractSummary = 3,

    /// <summary>The instructor workload (FR-14.4).</summary>
    InstructorWorkload = 4,

    /// <summary>The staff activity (FR-14.5).</summary>
    StaffActivity = 5
}
