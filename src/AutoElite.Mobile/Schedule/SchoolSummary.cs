using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The figures of the school for a period, for an administrator or a manager: the leads by source with the conversion,
/// the money received and refunded, the contracts drawn up, the work of the instructors and the tasks of the staff,
/// read from the API or, without a network, from the device; and each of them as a report, a PDF document or an Excel
/// workbook, downloaded into the cache of the application, which is emptied when the user signs out, and opened in
/// another application. The whole school is counted; the website narrows them to a branch or a course.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class SchoolSummary(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads the statistics of the leads that arrived in the period.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The statistics.</returns>
    public Task<Cached<LeadStatistics>> ReadLeadsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        cache.GetAsync(
            KeyOf("leads", from, to),
            token => createApi().Api.V1.Analytics.Leads.GetAsync(
                request =>
                {
                    request.QueryParameters.From = DateOf(from);
                    request.QueryParameters.To = DateOf(to);
                },
                token),
            LeadStatistics.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the money received and refunded in each month of the period, per currency.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The revenue by month and currency.</returns>
    public Task<Cached<IReadOnlyList<MonthlyRevenue>>> ReadRevenueAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            KeyOf("revenue", from, to),
            token => createApi().Api.V1.Analytics.Revenue.GetAsync(
                request =>
                {
                    request.QueryParameters.From = DateOf(from);
                    request.QueryParameters.To = DateOf(to);
                },
                token),
            MonthlyRevenue.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads how the contracts drawn up in the period stand.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The summary of the contracts.</returns>
    public Task<Cached<ContractSummary>> ReadContractsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        cache.GetAsync(
            KeyOf("contracts", from, to),
            token => createApi().Api.V1.Analytics.Contracts.GetAsync(
                request =>
                {
                    request.QueryParameters.From = DateOf(from);
                    request.QueryParameters.To = DateOf(to);
                },
                token),
            ContractSummary.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the work of each instructor on the lessons that started in the period.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The workload of the instructors.</returns>
    public Task<Cached<IReadOnlyList<InstructorWorkloadItem>>> ReadWorkloadAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            KeyOf("instructor-workload", from, to),
            token => createApi().Api.V1.Analytics.InstructorWorkload.GetAsync(
                request =>
                {
                    request.QueryParameters.From = DateOf(from);
                    request.QueryParameters.To = DateOf(to);
                },
                token),
            InstructorWorkloadItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the tasks set, completed and overdue of each staff member in the period.</summary>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The activity of the staff.</returns>
    public Task<Cached<IReadOnlyList<StaffActivityItem>>> ReadStaffAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            KeyOf("staff-activity", from, to),
            token => createApi().Api.V1.Analytics.StaffActivity.GetAsync(
                request =>
                {
                    request.QueryParameters.From = DateOf(from);
                    request.QueryParameters.To = DateOf(to);
                },
                token),
            StaffActivityItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Downloads a report of the period and opens it in another application.</summary>
    /// <param name="kind">The figures of the report, as the API names its kinds, such as <c>Revenue</c>.</param>
    /// <param name="format">The PDF document or the Excel workbook.</param>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the report is handed to another application.</returns>
    public async Task OpenReportAsync(
        string kind, ReportFormat format, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var extension = format == ReportFormat.Excel ? "xlsx" : "pdf";
        var name = string.Create(CultureInfo.InvariantCulture, $"{kind}-{from:yyyyMMdd}-{to:yyyyMMdd}.{extension}");
        var folder = Path.Combine(DocumentFiles.Folder, "reports");
        var path = Path.Combine(folder, name);
        Directory.CreateDirectory(folder);
        await using (var content = await createApi().Api.V1.Analytics.Reports[kind].GetAsync(
            request =>
            {
                request.QueryParameters.From = DateOf(from);
                request.QueryParameters.To = DateOf(to);
                request.QueryParameters.Format = format;
            },
            cancellationToken) ?? throw new InvalidOperationException("The API answered without the report."))
        {
            var partial = path + ".part";
            await using (var target = File.Create(partial))
                await content.CopyToAsync(target, cancellationToken);

            File.Move(partial, path, overwrite: true);
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest(name, new ReadOnlyFile(path)));
    }

    private static Date DateOf(DateOnly day) => new(day.Year, day.Month, day.Day);

    private static string KeyOf(string figures, DateOnly from, DateOnly to) =>
        string.Create(CultureInfo.InvariantCulture, $"analytics/{figures}/{from:yyyy-MM-dd}-{to:yyyy-MM-dd}");
}
