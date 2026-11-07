using System.Diagnostics.CodeAnalysis;
using AutoElite.Application.Common.Events;
using AutoElite.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// Delivers waiting outbox messages to <see cref="IDomainEventPublisher"/>, each in a dependency injection scope and
/// a database transaction of its own (D-057).
/// </summary>
/// <remarks>
/// <para>
/// A message is claimed with <c>FOR UPDATE SKIP LOCKED</c>, so that processors of several instances share the work
/// without delivering a message twice at the same time. Its processed mark is saved by the publisher together with the
/// changes of the handlers, and the transaction commits both; if a handler fails, the transaction is rolled back,
/// and the failure is recorded on its own, with a pause that doubles with every failure. A message that fails
/// <see cref="OutboxOptions.MaxAttempts"/> times is given up and stays with its last error, for someone to look at.
/// </para>
/// <para>
/// The scope is not a request, so the host's <see cref="ICurrentUser"/> must report the system there
/// (<see cref="ICurrentUser.IsSystem"/>), as the handlers run on its behalf.
/// </para>
/// </remarks>
internal sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    /// <summary>Delivers up to a batch of the messages that are due, the longest waiting first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The messages taken, delivered or not; fewer than a batch when no more were due.</returns>
    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var taken = 0;
        while (taken < settings.BatchSize && await ProcessNextAsync(settings, cancellationToken))
            taken++;

        return taken;
    }

    /// <summary>Delivers the next due message, if any.</summary>
    /// <returns>Whether a message was taken.</returns>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any failure of a handler is recorded on the message and retried; the worker must keep running.")]
    private async Task<bool> ProcessNextAsync(OutboxOptions settings, CancellationToken cancellationToken)
    {
        (Guid Id, int Attempts, string Type) claimed;
        Exception failure;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>();
            await dbContext.BeginIfNoneAsync(cancellationToken);
            var message = await ClaimAsync(dbContext, settings.MaxAttempts, cancellationToken);
            if (message is null)
                return false;

            claimed = (message.Id, message.Attempts, message.Type);
            try
            {
                var domainEvent = OutboxSerializer.Deserialize(message.Type, message.Payload);
                message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
                await publisher.PublishAsync(domainEvent, cancellationToken);

                return true;
            }
            catch (Exception exception) when (!IsShutdown(exception, cancellationToken))
            {
                // Leaving the scope rolls the transaction back, the processed mark and the handlers' changes with it,
                // and releases the lock, which recording the failure needs.
                failure = exception;
            }
        }
        catch (Exception exception) when (!IsShutdown(exception, cancellationToken))
        {
            // The database could not be reached or the message not claimed; the worker tries again after its pause.
            LogClaimFailed(logger, exception);

            return false;
        }

        await RecordFailureAsync(claimed.Id, claimed.Attempts + 1, claimed.Type, failure, settings, cancellationToken);

        return true;
    }

    private static bool IsShutdown(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested;

    private async Task<OutboxMessage?> ClaimAsync(AutoEliteDbContext dbContext, int maxAttempts, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var claimed = await dbContext.Set<OutboxMessage>()
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE processed_on_utc IS NULL AND attempts < {maxAttempts} AND next_attempt_on_utc <= {utcNow}
                ORDER BY next_attempt_on_utc, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return claimed.SingleOrDefault();
    }

    /// <summary>Counts a failed delivery in a scope of its own, since the delivery's transaction was rolled back.</summary>
    private async Task RecordFailureAsync(
        Guid messageId,
        int attempts,
        string type,
        Exception exception,
        OutboxOptions settings,
        CancellationToken cancellationToken)
    {
        var error = exception.ToString();
        if (error.Length > OutboxMessage.MaxErrorLength)
            error = error[..OutboxMessage.MaxErrorLength];

        var nextAttemptOnUtc = timeProvider.GetUtcNow().UtcDateTime + settings.RetryDelayAfter(attempts);
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Set<OutboxMessage>()
            .Where(message => message.Id == messageId && message.ProcessedOnUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Attempts, attempts)
                    .SetProperty(message => message.NextAttemptOnUtc, nextAttemptOnUtc)
                    .SetProperty(message => message.LastError, error),
                cancellationToken);

        if (attempts >= settings.MaxAttempts)
            LogGaveUp(logger, exception, type, messageId, attempts);
        else
            LogDeliveryFailed(logger, exception, type, messageId, attempts, nextAttemptOnUtc);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Delivering {EventType} {MessageId} failed {Attempts} times; next attempt at {NextAttemptOnUtc:O}")]
    private static partial void LogDeliveryFailed(
        ILogger logger, Exception exception, string eventType, Guid messageId, int attempts, DateTime nextAttemptOnUtc);

    [LoggerMessage(Level = LogLevel.Error, Message = "Gave up delivering {EventType} {MessageId} after {Attempts} attempts")]
    private static partial void LogGaveUp(ILogger logger, Exception exception, string eventType, Guid messageId, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not claim an outbox message")]
    private static partial void LogClaimFailed(ILogger logger, Exception exception);
}
