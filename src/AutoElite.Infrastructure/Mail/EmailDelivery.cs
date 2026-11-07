using System.Diagnostics.CodeAnalysis;
using AutoElite.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Mail;

/// <summary>
/// Sends the queued emails that are due, one at a time, each in a scope and a database transaction of its own.
/// </summary>
/// <remarks>
/// An email is claimed with <c>FOR UPDATE SKIP LOCKED</c>, so that several instances never send it at the same time;
/// it is marked sent, and its body cleared, in the same transaction. If sending fails, the transaction is rolled back
/// and the failure recorded on its own, with a pause that doubles with every failure, until
/// <see cref="EmailOptions.MaxAttempts"/>. Should the commit fail after the server took the email, it is sent again
/// later: a recipient may get an email twice, but never loses one.
/// </remarks>
internal sealed partial class EmailDeliveryProcessor(
    IServiceScopeFactory scopeFactory,
    IEmailSender sender,
    TimeProvider timeProvider,
    IOptions<EmailOptions> options,
    ILogger<EmailDeliveryProcessor> logger)
{
    /// <summary>Sends the next due email, if any.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>Whether an email was taken, sent or not.</returns>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any failure of a send is recorded on the email and retried; the delivery must keep running.")]
    public async Task<bool> SendNextAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        (Guid Id, int Attempts) claimed;
        Exception failure;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>();
            await dbContext.BeginIfNoneAsync(cancellationToken);
            var email = await ClaimAsync(dbContext, settings.MaxAttempts, cancellationToken);
            if (email is null)
                return false;

            claimed = (email.Id, email.Attempts);
            try
            {
                await sender.SendAsync(email, cancellationToken);
                email.MarkSent(timeProvider.GetUtcNow().UtcDateTime);
                await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // Leaving the scope rolls the transaction back and releases the lock, which recording the failure needs.
                failure = exception;
            }
        }

        await RecordFailureAsync(claimed.Id, claimed.Attempts + 1, failure, settings, cancellationToken);

        return true;
    }

    private async Task<QueuedEmail?> ClaimAsync(AutoEliteDbContext dbContext, int maxAttempts, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // xmin is a system column, which * leaves out, but the row version of the entity is read from it.
        var claimed = await dbContext.Set<QueuedEmail>()
            .FromSql($"""
                SELECT *, xmin FROM queued_emails
                WHERE sent_at_utc IS NULL AND attempts < {maxAttempts} AND next_attempt_at_utc <= {utcNow}
                ORDER BY next_attempt_at_utc, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return claimed.SingleOrDefault();
    }

    /// <summary>Counts a failed send in a scope of its own; the error names the failure, never the email's text.</summary>
    private async Task RecordFailureAsync(
        Guid emailId, int attempts, Exception exception, EmailOptions settings, CancellationToken cancellationToken)
    {
        var error = $"{exception.GetType().FullName}: {exception.Message}";
        if (error.Length > QueuedEmail.MaxErrorLength)
            error = error[..QueuedEmail.MaxErrorLength];

        var nextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime + settings.RetryDelayAfter(attempts);
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Set<QueuedEmail>()
            .Where(email => email.Id == emailId && email.SentAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(email => email.Attempts, attempts)
                    .SetProperty(email => email.NextAttemptAtUtc, nextAttemptAtUtc)
                    .SetProperty(email => email.LastError, error),
                cancellationToken);

        if (attempts >= settings.MaxAttempts)
            LogGaveUp(logger, exception, emailId, attempts);
        else
            LogSendFailed(logger, exception, emailId, attempts, nextAttemptAtUtc);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Sending email {EmailId} failed {Attempts} times; next attempt at {NextAttemptAtUtc:O}")]
    private static partial void LogSendFailed(
        ILogger logger, Exception exception, Guid emailId, int attempts, DateTime nextAttemptAtUtc);

    [LoggerMessage(Level = LogLevel.Error, Message = "Gave up sending email {EmailId} after {Attempts} attempts")]
    private static partial void LogGaveUp(ILogger logger, Exception exception, Guid emailId, int attempts);
}

/// <summary>
/// Sends the queued emails for as long as the host runs: one after another while there are due ones, otherwise after
/// <see cref="EmailOptions.PollingInterval"/>. A failed run is logged and the delivery carries on after the pause.
/// </summary>
internal sealed partial class EmailDeliveryWorker(
    EmailDeliveryProcessor processor,
    TimeProvider timeProvider,
    IOptions<EmailOptions> options,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The delivery outlives any failure of a run, such as a lost database; the next run tries again.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var sent = false;
            try
            {
                sent = await processor.SendNextAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogRunFailed(logger, exception);
            }

            if (!sent)
                await Task.Delay(options.Value.PollingInterval, timeProvider, stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A run of the email delivery failed")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
