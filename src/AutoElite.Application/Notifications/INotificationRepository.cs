namespace AutoElite.Application.Notifications;

/// <summary>Loads and adds notifications for commands.</summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>Marks every unread notification of the user as read at the time, in one statement.</summary>
    /// <param name="recipientId">The user.</param>
    /// <param name="utcNow">The time of reading.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The number of notifications marked.</returns>
    Task<int> MarkAllAsReadAsync(Guid recipientId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the unread notifications of the user about the record that were created no later than a time as read,
    /// in one statement.
    /// </summary>
    /// <param name="recipientId">The user.</param>
    /// <param name="subject">The record the notifications are about.</param>
    /// <param name="createdUpToUtc">The latest creation time of the notifications to mark.</param>
    /// <param name="utcNow">The time of reading.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The number of notifications marked.</returns>
    Task<int> MarkReadAboutAsync(
        Guid recipientId, EntityReference subject, DateTime createdUpToUtc, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Finds which of the users still have an unread notification of the type about the record.</summary>
    /// <param name="recipientIds">The users to check.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="subject">The record the notification is about.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The identifiers of the users who have one.</returns>
    Task<IReadOnlySet<Guid>> GetRecipientsWithUnreadAsync(
        IReadOnlyCollection<Guid> recipientIds,
        NotificationType type,
        EntityReference subject,
        CancellationToken cancellationToken = default);
}
