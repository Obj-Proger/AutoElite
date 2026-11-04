namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Hands emails to a background job that delivers them with retries, so a slow or unavailable mail server never
/// fails or delays a request.
/// </summary>
/// <remarks>
/// The email is stored in the current unit of work and handed to delivery only after it commits. A request that
/// fails therefore sends nothing, and a domain event applied once sends its email once.
/// </remarks>
public interface IEmailQueue
{
    /// <summary>Adds the email to the unit of work for delivery after it commits.</summary>
    /// <param name="message">The email to deliver.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
