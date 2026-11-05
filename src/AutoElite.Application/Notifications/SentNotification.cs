namespace AutoElite.Application.Notifications;

/// <summary>A notification that was created, as far as checking for repeats needs it.</summary>
/// <param name="RecipientId">The recipient.</param>
/// <param name="Subject">The record the notification is about.</param>
/// <param name="CreatedAtUtc">When the notification was created.</param>
public sealed record SentNotification(Guid RecipientId, EntityReference Subject, DateTime CreatedAtUtc);
