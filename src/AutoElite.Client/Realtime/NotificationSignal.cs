namespace AutoElite.Client.Realtime;

/// <summary>That a new notification of the signed-in user exists, which the client reads from the API.</summary>
/// <param name="NotificationId">The notification.</param>
public sealed record NotificationSignal(Guid NotificationId);
