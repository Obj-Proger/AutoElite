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
}
