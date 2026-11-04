using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Notifications.GetMyNotifications;

/// <summary>Reads the caller's page.</summary>
internal sealed class GetMyNotificationsQueryHandler(INotificationQueries notificationQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyNotificationsQuery, PagedList<NotificationItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<NotificationItem>>> Handle(GetMyNotificationsQuery query, CancellationToken cancellationToken) =>
        await notificationQueries.GetPageAsync(currentUser.GetRequiredUserId(), query.IsRead, PageRequest.From(query), cancellationToken);
}
