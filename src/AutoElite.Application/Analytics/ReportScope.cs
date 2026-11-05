namespace AutoElite.Application.Analytics;

/// <summary>What a report covers, resolved from the query and the caller.</summary>
/// <param name="BranchId">The branch to report on, or <see langword="null"/> for the whole school.</param>
/// <param name="Period">The period in UTC: from the start of the first day to the end of the last day, local time.</param>
/// <param name="TimeZone">The time zone of the branch or the school; months and days are counted in it.</param>
public sealed record ReportScope(Guid? BranchId, TimeInterval Period, TimeZoneInfo TimeZone);
