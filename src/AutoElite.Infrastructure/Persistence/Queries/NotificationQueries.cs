using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Notifications;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads the notifications of a user without tracking. The columns of a missing subject are empty, so reading them as
/// nullable values gives no subject without comparing the complex property with <see langword="null"/>.
/// </summary>
internal sealed class NotificationQueries(AutoEliteDbContext dbContext) : INotificationQueries
{
    /// <inheritdoc />
    public async Task<PagedList<NotificationItem>> GetPageAsync(
        Guid recipientId, bool? isRead, PageRequest page, CancellationToken cancellationToken = default)
    {
        var notifications = Of(recipientId);
        if (isRead is { } read)
        {
            notifications = read
                ? notifications.Where(notification => notification.ReadAtUtc != null)
                : notifications.Where(notification => notification.ReadAtUtc == null);
        }

        var (rows, totalCount) = await notifications
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => new NotificationItem(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                (EntityKind?)notification.Subject!.Kind,
                (Guid?)notification.Subject!.Id,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ReadPageAsync(page, cancellationToken);

        return new PagedList<NotificationItem>(rows, page, totalCount);
    }

    /// <inheritdoc />
    public Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default) =>
        Of(recipientId).CountAsync(notification => notification.ReadAtUtc == null, cancellationToken);

    private IQueryable<Notification> Of(Guid recipientId) =>
        dbContext.Set<Notification>().AsNoTracking().Where(notification => notification.RecipientId == recipientId);
}
