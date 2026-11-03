using AutoElite.Domain.Events;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A message in a <see cref="Chat"/>: text, attachments or both (FR-9.4). Kept as its own aggregate because a chat can
/// hold many messages.
/// </summary>
/// <remarks>
/// The text is stored encrypted (FR-9.3): the persistence layer encrypts it on save and decrypts it on load, so the
/// domain works with plain text and never sees the key. Attachments are documents of the document module referenced
/// by identifier. Deleting a message removes its text and attachments for everyone and keeps only the fact that a
/// message was deleted (FR-9.5).
/// </remarks>
public sealed class ChatMessage : AuditableEntity
{
    /// <summary>The most attachments one message may have.</summary>
    public const int MaxAttachments = 10;

    private const int MaxTextLength = 4000;

    private readonly List<Guid> _attachmentIds = [];

    /// <summary>Gets the identifier of the chat.</summary>
    public Guid ChatId { get; private set; }

    /// <summary>Gets the identifier of the sender.</summary>
    public Guid SenderId { get; private set; }

    /// <summary>Gets the text, or <see langword="null"/> if the message has only attachments or was deleted.</summary>
    public string? Text { get; private set; }

    /// <summary>Gets the identifiers of the attached documents; empty if there are none or the message was deleted.</summary>
    public IReadOnlyList<Guid> AttachmentIds => _attachmentIds.AsReadOnly();

    /// <summary>Gets the UTC time the message was sent.</summary>
    public DateTime SentAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the sender deleted the message.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Gets the UTC time the message was deleted, if it was.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    private ChatMessage(Guid id, Guid chatId, Guid senderId, string? text, DateTime sentAtUtc) : base(id)
    {
        ChatId = chatId;
        SenderId = senderId;
        Text = text;
        SentAtUtc = sentAtUtc;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private ChatMessage()
    {
    }

    /// <summary>Sends a message to a chat and raises <see cref="ChatMessageSentDomainEvent"/> for real-time delivery and notifications.</summary>
    /// <param name="chat">The chat; the sender must be a current member.</param>
    /// <param name="senderId">The user sending the message.</param>
    /// <param name="text">The text, if any.</param>
    /// <param name="attachmentIds">The attached documents, up to <see cref="MaxAttachments"/>, without repetitions.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The message, or a failure if the sender is not a member, the message is empty, or an argument is invalid.</returns>
    public static Result<ChatMessage> Send(Chat chat, Guid senderId, string? text, IReadOnlyCollection<Guid> attachmentIds, DateTime utcNow)
    {
        if (!chat.IsCurrentMember(senderId))
            return Result.Failure<ChatMessage>(DomainErrors.ChatMessage.SenderNotMember);

        var textResult = Guard.OptionalText(text, MaxTextLength, DomainErrors.ChatMessage.TextTooLong);
        if (textResult.IsFailure)
            return Result.Failure<ChatMessage>(textResult.Error);

        if (textResult.Value is null && attachmentIds.Count == 0)
            return Result.Failure<ChatMessage>(DomainErrors.ChatMessage.Empty);

        if (attachmentIds.Count > MaxAttachments)
            return Result.Failure<ChatMessage>(DomainErrors.ChatMessage.TooManyAttachments);

        if (attachmentIds.Contains(Guid.Empty) || attachmentIds.Distinct().Count() != attachmentIds.Count)
            return Result.Failure<ChatMessage>(DomainErrors.ChatMessage.InvalidAttachments);

        var message = new ChatMessage(Guid.CreateVersion7(), chat.Id, senderId, textResult.Value, utcNow);
        message._attachmentIds.AddRange(attachmentIds);
        message.RaiseDomainEvent(new ChatMessageSentDomainEvent(chat.Id, message.Id, senderId, utcNow));

        return message;
    }

    /// <summary>
    /// Deletes the message for everyone, removing its text and attachments, and raises <see cref="ChatMessageDeletedDomainEvent"/>.
    /// </summary>
    /// <param name="userId">The user deleting the message; only the sender may.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the user is not the sender or the message is already deleted.</returns>
    public Result Delete(Guid userId, DateTime utcNow)
    {
        if (userId != SenderId)
            return Result.Failure(DomainErrors.ChatMessage.NotSender);

        if (IsDeleted)
            return Result.Failure(DomainErrors.ChatMessage.AlreadyDeleted);

        IsDeleted = true;
        DeletedAtUtc = utcNow;
        Text = null;
        _attachmentIds.Clear();
        RaiseDomainEvent(new ChatMessageDeletedDomainEvent(ChatId, Id, utcNow));

        return Result.Success();
    }
}
