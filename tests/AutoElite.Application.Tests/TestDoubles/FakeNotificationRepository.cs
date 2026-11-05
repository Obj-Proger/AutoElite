using AutoElite.Application.Notifications;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

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

    public Task<int> MarkReadAboutAsync(
        Guid recipientId, EntityReference subject, DateTime createdUpToUtc, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var unread = Items.Where(notification => notification.RecipientId == recipientId && !notification.IsRead
            && notification.Subject == subject && notification.CreatedAtUtc <= createdUpToUtc).ToList();
        foreach (var notification in unread)
            notification.MarkAsRead(utcNow);

        return Task.FromResult(unread.Count);
    }

    public Task<IReadOnlySet<Guid>> GetRecipientsWithUnreadAsync(
        IReadOnlyCollection<Guid> recipientIds,
        NotificationType type,
        EntityReference subject,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<Guid>>(Items
            .Where(notification => recipientIds.Contains(notification.RecipientId) && !notification.IsRead
                && notification.Type == type && notification.Subject == subject)
            .Select(notification => notification.RecipientId)
            .ToHashSet());
}
