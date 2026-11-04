namespace AutoElite.Application.Notifications.GetUnreadNotificationCount;

/// <summary>Counts the caller's unread notifications.</summary>
internal sealed class GetUnreadNotificationCountQueryHandler(INotificationQueries notificationQueries, ICurrentUser currentUser)
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    /// <inheritdoc />
    public async Task<Result<int>> Handle(GetUnreadNotificationCountQuery query, CancellationToken cancellationToken) =>
        await notificationQueries.CountUnreadAsync(currentUser.GetRequiredUserId(), cancellationToken);
}
