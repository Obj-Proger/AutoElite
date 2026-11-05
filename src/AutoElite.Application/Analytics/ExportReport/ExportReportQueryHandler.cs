using System.Globalization;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Analytics.ExportReport;

/// <summary>
/// Reads the report, lays it out in the caller's language with the period, the branch and the time it was made, and
/// renders it.
/// </summary>
internal sealed class ExportReportQueryHandler(
    ReportScopes reportScopes,
    IAnalyticsQueries analyticsQueries,
    IReportRenderer reportRenderer,
    IRepository<Branch> branchRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<ExportReportQuery, ReportFile>
{
    /// <inheritdoc />
    public async Task<Result<ReportFile>> Handle(ExportReportQuery query, CancellationToken cancellationToken)
    {
        var scopeResult = await reportScopes.ResolveAsync(query, cancellationToken);
        if (scopeResult.IsFailure)
            return Result.Failure<ReportFile>(scopeResult.Error);

        var scope = scopeResult.Value;
        var caller = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);
        var culture = (caller?.PreferredLanguage ?? Language.Russian).ToCulture();
        var details = await DetailsAsync(query, scope, culture, cancellationToken);

        var document = query.Kind switch
        {
            ReportKind.LeadStatistics =>
                ReportDocuments.Leads(await analyticsQueries.GetLeadStatisticsAsync(scope, cancellationToken), details, culture),
            ReportKind.Revenue =>
                ReportDocuments.Revenue(await analyticsQueries.GetRevenueAsync(scope, cancellationToken), details, culture),
            ReportKind.ContractSummary => ReportDocuments.Contracts(
                await analyticsQueries.GetContractSummaryAsync(scope, query.CourseId, cancellationToken), details, culture),
            ReportKind.InstructorWorkload => ReportDocuments.Instructors(
                await analyticsQueries.GetInstructorWorkloadAsync(scope, cancellationToken), details, culture),
            _ => ReportDocuments.Staff(await analyticsQueries.GetStaffActivityAsync(scope, cancellationToken), details, culture)
        };

        var content = await reportRenderer.RenderAsync(document, query.Format, cancellationToken);

        return new ReportFile(FileName(query), ContentType(query.Format), content);
    }

    private static string FileName(ExportReportQuery query)
    {
        var name = query.Kind switch
        {
            ReportKind.LeadStatistics => "leads",
            ReportKind.Revenue => "revenue",
            ReportKind.ContractSummary => "contracts",
            ReportKind.InstructorWorkload => "instructor-workload",
            _ => "staff-activity"
        };
        var extension = query.Format == ReportFormat.Pdf ? "pdf" : "xlsx";

        return string.Create(CultureInfo.InvariantCulture, $"{name}-{query.From:yyyy-MM-dd}-{query.To:yyyy-MM-dd}.{extension}");
    }

    private static string ContentType(ReportFormat format) =>
        format == ReportFormat.Pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private async Task<IReadOnlyList<string>> DetailsAsync(
        ExportReportQuery query, ReportScope scope, CultureInfo culture, CancellationToken cancellationToken)
    {
        var branch = scope.BranchId is { } branchId ? await branchRepository.GetByIdAsync(branchId, cancellationToken) : null;
        var createdAt = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, scope.TimeZone);

        return
        [
            ReportTexts.Format(ReportTexts.Period, culture, query.From, query.To),
            branch is null
                ? ReportTexts.Format(ReportTexts.WholeSchool, culture)
                : ReportTexts.Format(ReportTexts.Branch, culture, branch.Name),
            ReportTexts.Format(ReportTexts.CreatedAt, culture, createdAt)
        ];
    }
}
