using AutoElite.Infrastructure.Persistence;

namespace AutoElite.Infrastructure.Mail;

/// <summary>
/// Queues emails in the database context, so that the unit of work stores them with the rest of the change and a
/// failed request queues nothing.
/// </summary>
internal sealed class EmailQueue(AutoEliteDbContext dbContext, TimeProvider timeProvider) : IEmailQueue
{
    /// <inheritdoc />
    public Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.Add(QueuedEmail.Create(message, timeProvider.GetUtcNow().UtcDateTime));

        return Task.CompletedTask;
    }
}
