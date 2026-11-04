namespace AutoElite.Application.Chats;

/// <summary>Reads chats and messages for queries.</summary>
public interface IChatQueries
{
    /// <summary>Determines whether the user is a current member of the chat.</summary>
    /// <param name="chatId">The identifier of the chat.</param>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> IsCurrentMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the chats the user is a current member of, the latest message first and chats without messages last. A
    /// user has few chats, so the list is not paged.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<ChatListItem>> GetOfUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads up to <paramref name="limit"/> messages of the chat, the latest first, with the text decrypted. With a
    /// cursor it reads the messages sent before that message, by the time sent and then by identifier.
    /// </summary>
    /// <param name="chatId">The identifier of the chat.</param>
    /// <param name="beforeMessageId">The oldest message already read, or <see langword="null"/> for the latest messages.</param>
    /// <param name="limit">The number of messages to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<ChatMessageItem>> GetMessagesAsync(
        Guid chatId, Guid? beforeMessageId, int limit, CancellationToken cancellationToken = default);

    /// <summary>Reads the current members of the chat, administrators first and then by name.</summary>
    /// <param name="chatId">The identifier of the chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<ChatMemberItem>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default);
}
