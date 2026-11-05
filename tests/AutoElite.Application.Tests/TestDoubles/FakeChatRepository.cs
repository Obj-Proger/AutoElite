using AutoElite.Application.Chats;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeChatRepository : InMemoryRepository<Chat>, IChatRepository
{
    public Task<Chat?> GetDirectAsync(string directKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(chat => chat.DirectKey == directKey));

    public Task<Chat?> GetOfStudyGroupAsync(Guid studyGroupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(chat => chat.StudyGroupId == studyGroupId));

    /// <summary>Gets the messages whose attachments <see cref="IsAnyAttachedAsync"/> looks at; none if not given.</summary>
    public InMemoryRepository<ChatMessage>? Messages { get; init; }

    public Task<bool> IsAnyAttachedAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(Messages?.Items.Any(message => message.AttachmentIds.Any(documentIds.Contains)) ?? false);
}
