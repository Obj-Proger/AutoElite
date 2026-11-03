using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>Raised when a notification is created; it is pushed to the recipient in real time and may be emailed.</summary>
/// <param name="NotificationId">The identifier of the notification.</param>
/// <param name="RecipientId">The identifier of the recipient.</param>
/// <param name="Type">What the notification is about.</param>
/// <param name="OccurredOnUtc">The UTC time of creation.</param>
public sealed record NotificationCreatedDomainEvent(Guid NotificationId, Guid RecipientId, NotificationType Type, DateTime OccurredOnUtc) : IDomainEvent;
