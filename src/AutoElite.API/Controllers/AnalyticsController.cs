using AutoElite.API.Common;
using AutoElite.Application.Analytics;
using AutoElite.Application.Analytics.ExportReport;
using AutoElite.Application.Analytics.GetContractSummary;
using AutoElite.Application.Analytics.GetInstructorWorkload;
using AutoElite.Application.Analytics.GetLeadStatistics;
using AutoElite.Application.Analytics.GetRevenue;
using AutoElite.Application.Analytics.GetStaffActivity;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The figures of the school for managers and administrators, and their reports as PDF documents and Excel
/// workbooks. A period runs from its first to its last day, both included, in the time zone of the school; a manager
/// sees their own branch, an administrator the whole school or one branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/analytics")]
public sealed class AnalyticsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Counts the leads that arrived in the period by source and by current status, with the conversion.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The statistics of the leads.</returns>
    [HttpGet("leads")]
    [ProducesResponseType<LeadStatistics>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeadStatistics(
        DateOnly from, DateOnly to, Guid? branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetLeadStatisticsQuery(from, to, branchId), cancellationToken));

    /// <summary>Sums the money received and refunded in each month of the period, separately for each currency.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The revenue by month and currency.</returns>
    [HttpGet("revenue")]
    [ProducesResponseType<IReadOnlyList<MonthlyRevenue>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenue(DateOnly from, DateOnly to, Guid? branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetRevenueQuery(from, to, branchId), cancellationToken));

    /// <summary>
    /// Counts the contracts drawn up in the period by their current status, with the share of successful completion and
    /// the average quality score.
    /// </summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="courseId">The course of the contracts.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The summary of the contracts.</returns>
    [HttpGet("contracts")]
    [ProducesResponseType<ContractSummary>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractSummary(
        DateOnly from, DateOnly to, Guid? branchId, Guid? courseId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetContractSummaryQuery(from, to, branchId, courseId), cancellationToken));

    /// <summary>
    /// Shows the work of each instructor on the lessons that started in the period: lessons, hours, cancellations,
    /// no-shows and the average rating.
    /// </summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The workload of the instructors.</returns>
    [HttpGet("instructor-workload")]
    [ProducesResponseType<IReadOnlyList<InstructorWorkloadItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstructorWorkload(
        DateOnly from, DateOnly to, Guid? branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetInstructorWorkloadQuery(from, to, branchId), cancellationToken));

    /// <summary>Counts the tasks set, completed and overdue of each staff member in the period.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The activity of the staff.</returns>
    [HttpGet("staff-activity")]
    [ProducesResponseType<IReadOnlyList<StaffActivityItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaffActivity(
        DateOnly from, DateOnly to, Guid? branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetStaffActivityQuery(from, to, branchId), cancellationToken));

    /// <summary>
    /// Downloads a report as a PDF document or an Excel workbook in the language of the request; it covers the same
    /// data as the figures of its kind.
    /// </summary>
    /// <param name="kind">The report.</param>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="format">The file format.</param>
    /// <param name="branchId">The branch; the whole school when not given.</param>
    /// <param name="courseId">For the report of the contracts, the course of the contracts.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The file of the report.</returns>
    [HttpGet("reports/{kind}")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/pdf",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportReport(
        ReportKind kind,
        DateOnly from,
        DateOnly to,
        ReportFormat format,
        Guid? branchId,
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ExportReportQuery(kind, from, to, format, branchId, courseId), cancellationToken);
        if (result.IsFailure)
            return Failure(result.Error);

        Response.Headers.XContentTypeOptions = "nosniff";

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }
}
