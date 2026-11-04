namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Hands emails to a background job that delivers them with retries, so a slow or unavailable mail server never
/// fails or delays a request.
/// </summary>
public interface IEmailQueue
{
    /// <summary>Queues the email for delivery and returns as soon as it is queued.</summary>
    /// <param name="message">The email to deliver.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
