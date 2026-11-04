namespace AutoElite.Application.Notifications.MarkAllNotificationsRead;

/// <summary>Marks all notifications of the signed-in user as read and returns how many were unread.</summary>
public sealed record MarkAllNotificationsReadCommand : ICommand<int>;
