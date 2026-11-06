using AutoElite.Application.Chats;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads chats with all their memberships, in the order they began.</summary>
internal sealed class ChatRepository(AutoEliteDbContext dbContext) : Repository<Chat>(dbContext), IChatRepository
{
    /// <inheritdoc />
    public override Task<Chat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithMembers().FirstOrDefaultAsync(chat => chat.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Chat?> GetDirectAsync(string directKey, CancellationToken cancellationToken = default) =>
        WithMembers().FirstOrDefaultAsync(chat => chat.DirectKey == directKey, cancellationToken);

    /// <inheritdoc />
    public Task<Chat?> GetOfStudyGroupAsync(Guid studyGroupId, CancellationToken cancellationToken = default) =>
        WithMembers().FirstOrDefaultAsync(chat => chat.StudyGroupId == studyGroupId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAnyAttachedAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        List<Guid> ids = [.. documentIds];

        return DbContext.Set<ChatMessage>()
            .AnyAsync(message => message.AttachmentIds.Any(attachmentId => ids.Contains(attachmentId)), cancellationToken);
    }

    private IQueryable<Chat> WithMembers() =>
        Aggregates.Include(chat => chat.Members.OrderBy(member => member.JoinedAtUtc).ThenBy(member => member.Id));
}

/// <summary>Loads and adds chat messages; a message has no child entities.</summary>
internal sealed class ChatMessageRepository(AutoEliteDbContext dbContext) : Repository<ChatMessage>(dbContext);
