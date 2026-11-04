namespace AutoElite.Application.Notifications.GetUnreadNotificationCount;

/// <summary>Counts the unread notifications of the signed-in user, for the badge of the bell.</summary>
public sealed record GetUnreadNotificationCountQuery : IQuery<int>;
