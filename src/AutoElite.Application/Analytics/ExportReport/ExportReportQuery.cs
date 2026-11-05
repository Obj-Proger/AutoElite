namespace AutoElite.Application.Analytics.ExportReport;

/// <summary>
/// Exports a report as a PDF document or an Excel workbook (FR-14.6), in the language of the caller. The report covers
/// the same data as its query, for example <see cref="GetRevenue.GetRevenueQuery"/>.
/// </summary>
/// <param name="Kind">The report.</param>
/// <param name="From">The first day of the period.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="Format">The file format.</param>
/// <param name="BranchId">The branch to report on; the whole school if not given, a manager's own branch for them.</param>
/// <param name="CourseId">For the contract report, only contracts for this course, if given.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ExportReportQuery(
    ReportKind Kind,
    DateOnly From,
    DateOnly To,
    ReportFormat Format,
    Guid? BranchId = null,
    Guid? CourseId = null) : IQuery<ReportFile>, IReportQuery;
