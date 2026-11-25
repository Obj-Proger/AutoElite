using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The figures of the school for a period, for an administrator or a manager: the leads by source with the conversion,
/// the money received and refunded, the contracts drawn up, the work of the instructors and the tasks of the staff,
/// read from the API or, without a network, from the device. The whole school is counted; the website narrows them to
/// a branch or a course.
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

    private static Date DateOf(DateOnly day) => new(day.Year, day.Month, day.Day);

    private static string KeyOf(string figures, DateOnly from, DateOnly to) =>
        string.Create(CultureInfo.InvariantCulture, $"analytics/{figures}/{from:yyyy-MM-dd}-{to:yyyy-MM-dd}");
}
