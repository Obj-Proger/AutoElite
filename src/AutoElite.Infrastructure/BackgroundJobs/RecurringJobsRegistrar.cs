using Hangfire;
using Microsoft.Extensions.Hosting;

namespace AutoElite.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules the recurring jobs when the host starts, replacing their earlier schedules, so that the schedule in the
/// code is the one in force; times are UTC.
/// </summary>
internal sealed class RecurringJobsRegistrar(IRecurringJobManager recurringJobs) : IHostedService
{
    /// <summary>The identifier of the reminders job.</summary>
    public const string SendRemindersJobId = "send-reminders";

    /// <summary>The identifier of the overdue tasks job.</summary>
    public const string RemindOverdueStaffTasksJobId = "remind-overdue-staff-tasks";

    /// <summary>The identifier of the clean-up job.</summary>
    public const string CleanUpJobId = "clean-up";

    private static readonly RecurringJobOptions _utc = new() { TimeZone = TimeZoneInfo.Utc };

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobs.AddOrUpdate<SystemJobs>(
            SendRemindersJobId, jobs => jobs.SendRemindersAsync(CancellationToken.None), Cron.MinuteInterval(5), _utc);
        recurringJobs.AddOrUpdate<SystemJobs>(
            RemindOverdueStaffTasksJobId,
            jobs => jobs.RemindOverdueStaffTasksAsync(CancellationToken.None),
            Cron.MinuteInterval(5),
            _utc);
        recurringJobs.AddOrUpdate<MaintenanceJobs>(
            CleanUpJobId, jobs => jobs.CleanUpAsync(CancellationToken.None), Cron.Daily(3), _utc);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
