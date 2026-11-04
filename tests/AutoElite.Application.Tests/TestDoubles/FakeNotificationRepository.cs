using AutoElite.Application.Notifications;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeNotificationRepository : InMemoryRepository<Notification>, INotificationRepository
{
    public Task<int> MarkAllAsReadAsync(Guid recipientId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var unread = Items.Where(notification => notification.RecipientId == recipientId && !notification.IsRead).ToList();
        foreach (var notification in unread)
            notification.MarkAsRead(utcNow);

        return Task.FromResult(unread.Count);
    }
}
