using AutoElite.Infrastructure.Mail;
using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence;
using Hangfire;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.BackgroundJobs;

/// <summary>The daily clean-up of the records that served their purpose.</summary>
/// <param name="dbContext">The database context of the job's scope.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="options">The settings of the jobs.</param>
internal sealed class MaintenanceJobs(
    AutoEliteDbContext dbContext, TimeProvider timeProvider, IOptions<BackgroundJobsOptions> options)
{
    /// <summary>
    /// Removes the outbox messages delivered and the emails sent longer than
    /// <see cref="BackgroundJobsOptions.RetentionPeriod"/> ago; messages that wait or were given up stay, for someone to
    /// look at.
    /// </summary>
    /// <param name="cancellationToken">The token Hangfire cancels when the server stops.</param>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(60 * 60)]
    public async Task CleanUpAsync(CancellationToken cancellationToken)
    {
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime - options.Value.RetentionPeriod;
        await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<QueuedEmail>()
            .Where(email => email.SentAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
