using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>An in-app notification for one user (FR-10.1, FR-10.2).</summary>
/// <remarks>
/// The title and body are written by the application layer in the recipient's language when the notification is
/// created, so the same text can also be sent by email. Marking a notification read is idempotent, so "mark all as
/// read" and repeated clicks never fail.
/// </remarks>
public sealed class Notification : BaseEntity
{
    private const int MaxTitleLength = 200;
    private const int MaxBodyLength = 2000;

    /// <summary>Gets the identifier of the recipient.</summary>
    public Guid RecipientId { get; private set; }

    /// <summary>Gets what the notification is about.</summary>
    public NotificationType Type { get; private set; }

    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Gets the body, if any.</summary>
    public string? Body { get; private set; }

    /// <summary>Gets the object the notification links to, if any.</summary>
    public EntityReference? Subject { get; private set; }

    /// <summary>Gets the UTC time the notification was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the recipient read the notification, or <see langword="null"/> while unread.</summary>
    public DateTime? ReadAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the notification has been read.</summary>
    public bool IsRead => ReadAtUtc is not null;

    private Notification(Guid id, Guid recipientId, NotificationType type, string title, string? body, EntityReference? subject, DateTime createdAtUtc)
        : base(id)
    {
        RecipientId = recipientId;
        Type = type;
        Title = title;
        Body = body;
        Subject = subject;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Notification()
    {
    }

    /// <summary>Creates an unread notification and raises <see cref="NotificationCreatedDomainEvent"/> for real-time delivery.</summary>
    /// <param name="recipientId">The user to notify.</param>
    /// <param name="type">What the notification is about.</param>
    /// <param name="title">The title. Required.</param>
    /// <param name="body">The body, if any.</param>
    /// <param name="subject">The object to link to, if any.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The notification, or the first validation failure encountered.</returns>
    public static Result<Notification> Create(
        Guid recipientId, NotificationType type, string? title, string? body, EntityReference? subject, DateTime utcNow)
    {
        var recipientResult = Guard.NotEmpty(recipientId, DomainErrors.Notification.RecipientIdEmpty);
        if (recipientResult.IsFailure)
            return Result.Failure<Notification>(recipientResult.Error);

        var typeResult = Guard.DefinedEnum(type, DomainErrors.Notification.TypeNotDefined);
        if (typeResult.IsFailure)
            return Result.Failure<Notification>(typeResult.Error);

        var titleResult = Guard.RequiredText(title, MaxTitleLength, DomainErrors.Notification.TitleEmpty, DomainErrors.Notification.TitleTooLong);
        if (titleResult.IsFailure)
            return Result.Failure<Notification>(titleResult.Error);

        var bodyResult = Guard.OptionalText(body, MaxBodyLength, DomainErrors.Notification.BodyTooLong);
        if (bodyResult.IsFailure)
            return Result.Failure<Notification>(bodyResult.Error);

        var notification = new Notification(Guid.CreateVersion7(), recipientId, type, titleResult.Value, bodyResult.Value, subject, utcNow);
        notification.RaiseDomainEvent(new NotificationCreatedDomainEvent(notification.Id, recipientId, type, utcNow));

        return notification;
    }

    /// <summary>Marks the notification read. Does nothing if it is already read.</summary>
    public void MarkAsRead(DateTime utcNow) => ReadAtUtc ??= utcNow;
}
