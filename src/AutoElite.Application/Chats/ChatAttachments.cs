using AutoElite.Application.Documents;

namespace AutoElite.Application.Chats;

/// <summary>
/// Checks the attachments of a message. A file is first attached to the chat as a document
/// (<see cref="Documents.AttachDocument.AttachDocumentCommand"/>), then sent with one message; the domain checks the
/// number of attachments.
/// </summary>
internal sealed class ChatAttachments(IDocumentRepository documentRepository, IChatRepository chatRepository)
{
    /// <summary>
    /// Checks that every attachment is a document the sender attached to the chat and that none was sent before.
    /// </summary>
    /// <param name="chat">The chat of the message.</param>
    /// <param name="senderId">The sender.</param>
    /// <param name="documentIds">The identifiers of the documents to attach.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(
        Chat chat, Guid senderId, IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
            return Result.Success();

        var owner = EntityReference.Create(EntityKind.Chat, chat.Id).Value;
        var documents = await documentRepository.GetByIdsAsync(documentIds, cancellationToken);
        var valid = documents.Count == documentIds.Distinct().Count()
            && documents.All(document => document.Owner == owner && document.UploadedByUserId == senderId);
        if (!valid)
            return Result.Failure(ApplicationErrors.Chat.AttachmentInvalid);

        return await chatRepository.IsAnyAttachedAsync(documentIds, cancellationToken)
            ? Result.Failure(ApplicationErrors.Chat.AttachmentAlreadySent)
            : Result.Success();
    }

    /// <summary>Deletes the documents of a message's attachments, together with their files.</summary>
    /// <param name="documentIds">The attachments of the message, taken before the message was deleted.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task DeleteAsync(IReadOnlyCollection<Guid> documentIds, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
            return;

        foreach (var document in await documentRepository.GetByIdsAsync(documentIds, cancellationToken))
        {
            if (document.Delete(utcNow).IsSuccess)
                documentRepository.Remove(document);
        }
    }
}
