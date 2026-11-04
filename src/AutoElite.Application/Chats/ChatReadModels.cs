namespace AutoElite.Application.Chats;

/// <summary>A chat in the list of the signed-in user.</summary>
/// <param name="Id">The identifier of the chat.</param>
/// <param name="Kind">Personal or group.</param>
/// <param name="Title">The title of a group chat, or the full name of the other person of a personal chat.</param>
/// <param name="CounterpartId">The other person of a personal chat.</param>
/// <param name="StudyGroupId">The study group of the chat kept by the system, if any.</param>
/// <param name="LastMessageText">The text of the latest message, if any and not deleted.</param>
/// <param name="LastMessageSenderId">The sender of the latest message, if any.</param>
/// <param name="LastMessageAtUtc">When the latest message was sent, if any.</param>
/// <param name="UnreadCount">The messages of others sent after the user's read position and not deleted.</param>
/// <param name="IsMuted">Whether the user turned the notifications of the chat off.</param>
/// <param name="Role">The user's role in the chat.</param>
public sealed record ChatListItem(
    Guid Id,
    ChatKind Kind,
    string Title,
    Guid? CounterpartId,
    Guid? StudyGroupId,
    string? LastMessageText,
    Guid? LastMessageSenderId,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    bool IsMuted,
    ChatMemberRole Role);

/// <summary>A message of a chat.</summary>
/// <param name="Id">The identifier of the message.</param>
/// <param name="SenderId">The sender.</param>
/// <param name="SenderName">The full name of the sender.</param>
/// <param name="Text">The text, decrypted; <see langword="null"/> for a deleted message or one with attachments only.</param>
/// <param name="AttachmentIds">The documents attached, empty for a deleted message.</param>
/// <param name="SentAtUtc">When the message was sent.</param>
/// <param name="IsDeleted">Whether the sender deleted the message.</param>
public sealed record ChatMessageItem(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string? Text,
    IReadOnlyList<Guid> AttachmentIds,
    DateTime SentAtUtc,
    bool IsDeleted);

/// <summary>A current member of a chat.</summary>
/// <param name="UserId">The user.</param>
/// <param name="FullName">The full name of the user.</param>
/// <param name="Role">The role in the chat.</param>
/// <param name="JoinedAtUtc">When the user joined the chat.</param>
/// <param name="LastReadAtUtc">The read position of the user, if any.</param>
public sealed record ChatMemberItem(Guid UserId, string FullName, ChatMemberRole Role, DateTime JoinedAtUtc, DateTime? LastReadAtUtc);
