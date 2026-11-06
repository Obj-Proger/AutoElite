using AutoElite.Application.Chats;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads chats, members and messages without tracking. Texts are read as message entities, so that they pass through
/// the decrypting converter; the database only orders and counts them.
/// </summary>
internal sealed class ChatQueries(AutoEliteDbContext dbContext) : IChatQueries
{
    /// <inheritdoc />
    public Task<bool> IsCurrentMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default) =>
        CurrentMembers(chatId).AnyAsync(member => member.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatListItem>> GetOfUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var members = dbContext.Set<ChatMember>();
        var messages = dbContext.Set<ChatMessage>();
        var chats = await (
                from member in members.AsNoTracking()
                join chat in dbContext.Set<Chat>() on EF.Property<Guid>(member, ChatConfiguration.ChatKey) equals chat.Id
                where member.UserId == userId && member.LeftAtUtc == null
                select new
                {
                    chat.Id,
                    chat.Kind,
                    chat.Title,
                    chat.StudyGroupId,
                    member.IsMuted,
                    member.Role,
                    CounterpartId = chat.Kind == ChatKind.Direct
                        ? members
                            .Where(other => EF.Property<Guid>(other, ChatConfiguration.ChatKey) == chat.Id
                                && other.UserId != userId)
                            .Select(other => (Guid?)other.UserId)
                            .FirstOrDefault()
                        : null,
                    LastMessageId = messages
                        .Where(message => message.ChatId == chat.Id)
                        .OrderByDescending(message => message.SentAtUtc)
                        .ThenByDescending(message => message.Id)
                        .Select(message => (Guid?)message.Id)
                        .FirstOrDefault(),
                    UnreadCount = messages.Count(message => message.ChatId == chat.Id
                        && message.SenderId != userId
                        && !message.IsDeleted
                        && (member.LastReadAtUtc == null || message.SentAtUtc > member.LastReadAtUtc))
                })
            .ToListAsync(cancellationToken);

        List<Guid> lastIds = [.. chats.Where(chat => chat.LastMessageId != null).Select(chat => chat.LastMessageId!.Value)];
        var lastMessages = await messages.AsNoTracking()
            .Where(message => lastIds.Contains(message.Id))
            .ToDictionaryAsync(message => message.Id, cancellationToken);
        List<Guid> counterpartIds = [.. chats.Where(chat => chat.CounterpartId != null).Select(chat => chat.CounterpartId!.Value)];
        var counterpartNames = await dbContext.Set<User>().AsNoTracking()
            .Where(user => counterpartIds.Contains(user.Id))
            .Select(user => new { user.Id, user.FullName.LastName, user.FullName.FirstName, user.FullName.MiddleName })
            .ToDictionaryAsync(
                user => user.Id, user => PersonNames.Format(user.LastName, user.FirstName, user.MiddleName)!, cancellationToken);

        return
        [
            .. chats
                .Select(chat =>
                {
                    var last = chat.LastMessageId is { } lastId ? lastMessages[lastId] : null;
                    var title = chat.CounterpartId is { } counterpartId ? counterpartNames[counterpartId] : chat.Title;

                    return new ChatListItem(
                        chat.Id, chat.Kind, title ?? string.Empty, chat.CounterpartId, chat.StudyGroupId, last?.Text,
                        last?.SenderId, last?.SentAtUtc, chat.UnreadCount, chat.IsMuted, chat.Role);
                })
                .OrderByDescending(chat => chat.LastMessageAtUtc ?? DateTime.MinValue)
                .ThenByDescending(chat => chat.Id)
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageItem>> GetMessagesAsync(
        Guid chatId, Guid? beforeMessageId, int limit, CancellationToken cancellationToken = default)
    {
        var messages = dbContext.Set<ChatMessage>().AsNoTracking().Where(message => message.ChatId == chatId);
        if (beforeMessageId is { } cursorId)
        {
            var cursor = await messages
                .Where(message => message.Id == cursorId)
                .Select(message => new { message.SentAtUtc, message.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (cursor is null)
                return [];

            // A row comparison, which the index on the chat, the time sent and the identifier serves.
            var (cursorSentAtUtc, cursorMessageId) = (cursor.SentAtUtc, cursor.Id);
            messages = messages.Where(message => EF.Functions.LessThan(
                ValueTuple.Create(message.SentAtUtc, message.Id), ValueTuple.Create(cursorSentAtUtc, cursorMessageId)));
        }

        var found = await (
                from message in messages
                    .OrderByDescending(candidate => candidate.SentAtUtc)
                    .ThenByDescending(candidate => candidate.Id)
                    .Take(limit)
                join sender in dbContext.Set<User>() on message.SenderId equals sender.Id
                select new
                {
                    Message = message,
                    sender.FullName.LastName,
                    sender.FullName.FirstName,
                    sender.FullName.MiddleName
                })
            .ToListAsync(cancellationToken);

        return
        [
            .. found
                .OrderByDescending(row => row.Message.SentAtUtc)
                .ThenByDescending(row => row.Message.Id)
                .Select(row => new ChatMessageItem(
                    row.Message.Id, row.Message.SenderId, PersonNames.Format(row.LastName, row.FirstName, row.MiddleName)!,
                    row.Message.Text, [.. row.Message.AttachmentIds], row.Message.SentAtUtc, row.Message.IsDeleted))
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMemberItem>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var found = await (
                from member in CurrentMembers(chatId)
                join user in dbContext.Set<User>() on member.UserId equals user.Id
                select new
                {
                    member.UserId,
                    member.Role,
                    member.JoinedAtUtc,
                    member.LastReadAtUtc,
                    user.FullName.LastName,
                    user.FullName.FirstName,
                    user.FullName.MiddleName
                })
            .ToListAsync(cancellationToken);

        return
        [
            .. found
                .Select(member => new ChatMemberItem(
                    member.UserId, PersonNames.Format(member.LastName, member.FirstName, member.MiddleName)!, member.Role,
                    member.JoinedAtUtc, member.LastReadAtUtc))
                .OrderBy(member => member.Role == ChatMemberRole.Administrator ? 0 : 1)
                .ThenBy(member => member.FullName, StringComparer.CurrentCulture)
                .ThenBy(member => member.UserId)
        ];
    }

    private IQueryable<ChatMember> CurrentMembers(Guid chatId) =>
        dbContext.Set<ChatMember>().AsNoTracking()
            .Where(member => EF.Property<Guid>(member, ChatConfiguration.ChatKey) == chatId && member.LeftAtUtc == null);
}
