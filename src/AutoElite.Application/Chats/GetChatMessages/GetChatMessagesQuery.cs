namespace AutoElite.Application.Chats.GetChatMessages;

/// <summary>
/// Reads the messages of a chat of the signed-in user, the latest first, a batch at a time: the next batch starts
/// before the oldest message of the previous one, so new messages do not shift it.
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="BeforeMessageId">The oldest message of the previous batch; empty for the latest messages.</param>
/// <param name="Limit">The number of messages, from 1 to 100.</param>
public sealed record GetChatMessagesQuery(Guid ChatId, Guid? BeforeMessageId = null, int Limit = GetChatMessagesQuery.DefaultLimit)
    : IQuery<IReadOnlyList<ChatMessageItem>>
{
    /// <summary>The number of messages read when none is given.</summary>
    public const int DefaultLimit = 50;
}
