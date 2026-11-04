namespace AutoElite.Application.Notifications.MarkNotificationRead;

/// <summary>Hides the notifications of other users as not found and marks the caller's one as read.</summary>
internal sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkNotificationReadCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification is null || notification.RecipientId != currentUser.GetRequiredUserId())
            return Result.Failure(ApplicationErrors.NotFound<Notification>());

        notification.MarkAsRead(timeProvider.GetUtcNow().UtcDateTime);

        return Result.Success();
    }
}
