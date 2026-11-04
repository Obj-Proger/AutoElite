using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Notifications;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Records the page arguments and returns a fixed unread count; filtering is the infrastructure's job.</summary>
internal sealed class FakeNotificationQueries : INotificationQueries
{
    public (Guid RecipientId, bool? IsRead, PageRequest Page)? LastPageCall { get; private set; }

    public int UnreadCount { get; set; }

    public Task<PagedList<NotificationItem>> GetPageAsync(
        Guid recipientId, bool? isRead, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastPageCall = (recipientId, isRead, page);

        return Task.FromResult(PagedList<NotificationItem>.Empty(page));
    }

    public Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(UnreadCount);
}
