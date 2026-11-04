using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Notifications;

/// <summary>Reads notifications for queries.</summary>
public interface INotificationQueries
{
    /// <summary>Reads one page of the notifications of the user, the latest first.</summary>
    /// <param name="recipientId">The user.</param>
    /// <param name="isRead">Only read or only unread notifications; <see langword="null"/> for all.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<NotificationItem>> GetPageAsync(
        Guid recipientId, bool? isRead, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Counts the unread notifications of the user.</summary>
    /// <param name="recipientId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default);
}
