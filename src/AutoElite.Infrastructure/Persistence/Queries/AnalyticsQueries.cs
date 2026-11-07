using AutoElite.Application.Analytics;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads the reports without tracking. The database counts and sums; months are counted here in the report's time
/// zone, which needs the moments of the payments rather than SQL dates. A record belongs to the branch of the report
/// only when it names that branch.
/// </summary>
/// <remarks>
/// A task is overdue when it was completed after its deadline, or is still open and its deadline has passed; a period
/// that reaches into the future does not count open tasks that are not due yet.
/// </remarks>
internal sealed class AnalyticsQueries(AutoEliteDbContext dbContext, TimeProvider timeProvider) : IAnalyticsQueries
{
    /// <inheritdoc />
    public async Task<LeadStatistics> GetLeadStatisticsAsync(ReportScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var (startUtc, endUtc, branchId) = (scope.Period.StartUtc, scope.Period.EndUtc, scope.BranchId);
        var leads = dbContext.Set<Lead>().AsNoTracking()
            .Where(lead => lead.CreatedAtUtc >= startUtc && lead.CreatedAtUtc < endUtc);
        if (branchId is not null)
            leads = leads.Where(lead => lead.BranchId == branchId);

        var counts = await leads
            .GroupBy(lead => new { lead.Source, lead.Status })
            .Select(group => new { group.Key.Source, group.Key.Status, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(Func<LeadSource, LeadStatus, bool> matches) =>
            counts.Where(count => matches(count.Source, count.Status)).Sum(count => count.Count);

        return new LeadStatistics(
            Count((_, _) => true),
            Count((_, status) => status == LeadStatus.Converted),
            [
                .. Enum.GetValues<LeadSource>().Select(source => new LeadSourceStatistics(
                    source,
                    Count((found, _) => found == source),
                    Count((found, status) => found == source && status == LeadStatus.Converted)))
            ],
            [.. Enum.GetValues<LeadStatus>().Select(status => new LeadStatusCount(status, Count((_, found) => found == status)))]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MonthlyRevenue>> GetRevenueAsync(ReportScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var (startUtc, endUtc, branchId) = (scope.Period.StartUtc, scope.Period.EndUtc, scope.BranchId);
        var payments =
            from payment in dbContext.Set<Payment>().AsNoTracking()
            join contract in dbContext.Set<Contract>() on payment.ContractId equals contract.Id
            where branchId == null || contract.BranchId == branchId
            select payment;
        var moves = await payments
            .Where(paid => (paid.ConfirmedAtUtc >= startUtc && paid.ConfirmedAtUtc < endUtc)
                || (paid.RefundedAtUtc >= startUtc && paid.RefundedAtUtc < endUtc))
            .Select(paid => new
            {
                paid.Amount.Amount,
                paid.Amount.Currency,
                paid.ConfirmedAtUtc,
                paid.RefundedAtUtc
            })
            .ToListAsync(cancellationToken);

        bool InPeriod(DateTime? atUtc) => atUtc >= startUtc && atUtc < endUtc;

        var received = moves
            .Where(move => InPeriod(move.ConfirmedAtUtc))
            .Select(move => (Month: MonthOf(move.ConfirmedAtUtc!.Value, scope.TimeZone), move.Currency.Code, move.Amount, Refund: false));
        var refunded = moves
            .Where(move => InPeriod(move.RefundedAtUtc))
            .Select(move => (Month: MonthOf(move.RefundedAtUtc!.Value, scope.TimeZone), move.Currency.Code, move.Amount, Refund: true));

        return
        [
            .. received.Concat(refunded)
                .GroupBy(move => (move.Month.Year, move.Month.Month, move.Code))
                .OrderBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Month)
                .ThenBy(group => group.Key.Code, StringComparer.Ordinal)
                .Select(group => new MonthlyRevenue(
                    group.Key.Year,
                    group.Key.Month,
                    group.Key.Code,
                    group.Where(move => !move.Refund).Sum(move => move.Amount),
                    group.Where(move => move.Refund).Sum(move => move.Amount)))
        ];
    }

    /// <inheritdoc />
    public async Task<ContractSummary> GetContractSummaryAsync(
        ReportScope scope, Guid? courseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var (startUtc, endUtc, branchId) = (scope.Period.StartUtc, scope.Period.EndUtc, scope.BranchId);
        var contracts = dbContext.Set<Contract>().AsNoTracking()
            .Where(contract => contract.CreatedAtUtc >= startUtc && contract.CreatedAtUtc < endUtc);
        if (branchId is not null)
            contracts = contracts.Where(contract => contract.BranchId == branchId);

        if (courseId is not null)
            contracts = contracts.Where(contract => contract.CourseId == courseId);

        var byStatus = await contracts
            .GroupBy(contract => contract.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var averageQuality = await contracts
            .Where(contract => contract.QualityScore != null)
            .AverageAsync(contract => contract.QualityScore, cancellationToken);

        int Count(ContractStatus status) => byStatus.Where(found => found.Status == status).Sum(found => found.Count);

        return new ContractSummary(
            Count(ContractStatus.Active),
            Count(ContractStatus.Completed),
            Count(ContractStatus.Terminated),
            averageQuality is { } average ? decimal.Round(average, 2) : null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InstructorWorkloadItem>> GetInstructorWorkloadAsync(
        ReportScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var (startUtc, endUtc) = (scope.Period.StartUtc, scope.Period.EndUtc);
        var driving = await (
                from slot in dbContext.Set<DrivingSlot>().AsNoTracking()
                from booking in slot.Bookings
                where slot.Time.StartUtc >= startUtc && slot.Time.StartUtc < endUtc
                group booking by slot.InstructorId into lessons
                select new
                {
                    InstructorId = lessons.Key,
                    Completed = lessons.Count(lesson => lesson.Status == DrivingBookingStatus.Completed),
                    Hours = lessons.Where(lesson => lesson.Status == DrivingBookingStatus.Completed)
                        .Sum(lesson => lesson.ActualHours ?? 0m),
                    Cancelled = lessons.Count(lesson => lesson.Status == DrivingBookingStatus.Cancelled),
                    NoShows = lessons.Count(lesson => lesson.Status == DrivingBookingStatus.NoShow),
                    AverageRating = lessons.Average(lesson => (decimal?)lesson.StudentRating)
                })
            .ToListAsync(cancellationToken);
        var theory = await dbContext.Set<TheoryLesson>().AsNoTracking()
            .Where(lesson => lesson.Status == TheoryLessonStatus.Completed
                && lesson.Time.StartUtc >= startUtc
                && lesson.Time.StartUtc < endUtc)
            .Select(lesson => new { lesson.InstructorId, lesson.Time.StartUtc, lesson.Time.EndUtc })
            .ToListAsync(cancellationToken);

        var theoryByInstructor = theory
            .GroupBy(lesson => lesson.InstructorId)
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Hours: group.Sum(lesson => (decimal)(lesson.EndUtc - lesson.StartUtc).TotalHours)));
        var drivingByInstructor = driving.ToDictionary(row => row.InstructorId);
        var names = await StaffNamesAsync([.. drivingByInstructor.Keys.Union(theoryByInstructor.Keys)], scope, cancellationToken);

        return
        [
            .. names
                .Select(staff =>
                {
                    var ofInstructor = drivingByInstructor.GetValueOrDefault(staff.Id);
                    var (theoryCount, theoryHours) = theoryByInstructor.GetValueOrDefault(staff.Id);

                    return new InstructorWorkloadItem(
                        staff.Id, staff.Name, ofInstructor?.Completed ?? 0, ofInstructor?.Hours ?? 0m, ofInstructor?.Cancelled ?? 0,
                        ofInstructor?.NoShows ?? 0, ofInstructor?.AverageRating is { } rating ? decimal.Round(rating, 2) : null,
                        theoryCount, decimal.Round(theoryHours, 2));
                })
                .Where(item => item.DrivingLessons + item.DrivingCancellations + item.DrivingNoShows + item.TheoryLessons > 0)
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaffActivityItem>> GetStaffActivityAsync(
        ReportScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var (startUtc, endUtc) = (scope.Period.StartUtc, scope.Period.EndUtc);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var counts = await dbContext.Set<StaffTask>().AsNoTracking()
            .Where(task => (task.CreatedAtUtc >= startUtc && task.CreatedAtUtc < endUtc)
                || (task.CompletedAtUtc >= startUtc && task.CompletedAtUtc < endUtc)
                || (task.DueAtUtc >= startUtc && task.DueAtUtc < endUtc))
            .GroupBy(task => task.AssigneeId)
            .Select(tasks => new
            {
                AssigneeId = tasks.Key,
                Assigned = tasks.Count(task => task.CreatedAtUtc >= startUtc && task.CreatedAtUtc < endUtc),
                Completed = tasks.Count(task => task.Status == StaffTaskStatus.Done
                    && task.CompletedAtUtc >= startUtc
                    && task.CompletedAtUtc < endUtc),
                Overdue = tasks.Count(task => task.DueAtUtc >= startUtc
                    && task.DueAtUtc < endUtc
                    && (task.CompletedAtUtc == null ? task.DueAtUtc < utcNow : task.CompletedAtUtc > task.DueAtUtc))
            })
            .ToListAsync(cancellationToken);
        var byAssignee = counts.ToDictionary(count => count.AssigneeId);
        var names = await StaffNamesAsync([.. byAssignee.Keys], scope, cancellationToken);

        return
        [
            .. names.Select(staff =>
            {
                var count = byAssignee[staff.Id];

                return new StaffActivityItem(staff.Id, staff.Name, count.Assigned, count.Completed, count.Overdue);
            })
        ];
    }

    /// <summary>The month of a moment in the time zone of the report.</summary>
    private static (int Year, int Month) MonthOf(DateTime utc, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);

        return (local.Year, local.Month);
    }

    /// <summary>The names of the staff members of the report's branch among the given ones, ordered by name.</summary>
    private async Task<IReadOnlyList<(Guid Id, string Name)>> StaffNamesAsync(
        List<Guid> userIds, ReportScope scope, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var branchId = scope.BranchId;
        var users = dbContext.Set<User>().AsNoTracking().Where(user => userIds.Contains(user.Id));
        if (branchId is not null)
            users = users.Where(user => user.BranchId == branchId);

        var found = await users
            .Select(user => new { user.Id, user.FullName.LastName, user.FullName.FirstName, user.FullName.MiddleName })
            .ToListAsync(cancellationToken);

        return
        [
            .. found
                .Select(user => (user.Id, Name: PersonNames.Format(user.LastName, user.FirstName, user.MiddleName)!))
                .OrderBy(user => user.Name, StringComparer.CurrentCulture)
                .ThenBy(user => user.Id)
        ];
    }
}
