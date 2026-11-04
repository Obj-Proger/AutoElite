namespace AutoElite.Application.Notifications.MarkNotificationRead;

/// <summary>Marks a notification of the signed-in user as read; marking it again changes nothing.</summary>
/// <param name="NotificationId">The identifier of the notification.</param>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
