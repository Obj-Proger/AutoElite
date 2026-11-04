namespace AutoElite.Application.Notifications.MarkAllNotificationsRead;

/// <summary>Marks the caller's unread notifications in one statement.</summary>
internal sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    /// <inheritdoc />
    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken) =>
        await notificationRepository.MarkAllAsReadAsync(
            currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
}
