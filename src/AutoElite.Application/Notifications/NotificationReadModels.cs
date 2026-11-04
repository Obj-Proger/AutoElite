namespace AutoElite.Application.Notifications;

/// <summary>A notification of the signed-in user.</summary>
/// <param name="Id">The identifier of the notification.</param>
/// <param name="Type">The type.</param>
/// <param name="Title">The title in the user's language.</param>
/// <param name="Body">The text in the user's language, if any.</param>
/// <param name="SubjectKind">The kind of the record the notification is about, if any.</param>
/// <param name="SubjectId">The record the notification is about, if any.</param>
/// <param name="CreatedAtUtc">When the notification was created.</param>
/// <param name="ReadAtUtc">When the user read it, if they did.</param>
public sealed record NotificationItem(
    Guid Id,
    NotificationType Type,
    string Title,
    string? Body,
    EntityKind? SubjectKind,
    Guid? SubjectId,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
