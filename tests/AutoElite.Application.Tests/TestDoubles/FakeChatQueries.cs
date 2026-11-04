using AutoElite.Application.Chats;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded chats and records the message arguments; ordering and paging are the infrastructure's job.</summary>
internal sealed class FakeChatQueries : IChatQueries
{
    public HashSet<(Guid ChatId, Guid UserId)> Members { get; } = [];

    public List<ChatListItem> Chats { get; } = [];

    public (Guid ChatId, Guid? BeforeMessageId, int Limit)? LastMessagesCall { get; private set; }

    public Guid? LastUserId { get; private set; }

    public Task<bool> IsCurrentMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Members.Contains((chatId, userId)));

    public Task<IReadOnlyList<ChatListItem>> GetOfUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        LastUserId = userId;

        return Task.FromResult<IReadOnlyList<ChatListItem>>([.. Chats]);
    }

    public Task<IReadOnlyList<ChatMessageItem>> GetMessagesAsync(
        Guid chatId, Guid? beforeMessageId, int limit, CancellationToken cancellationToken = default)
    {
        LastMessagesCall = (chatId, beforeMessageId, limit);

        return Task.FromResult<IReadOnlyList<ChatMessageItem>>([]);
    }

    public Task<IReadOnlyList<ChatMemberItem>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChatMemberItem>>([]);
}
