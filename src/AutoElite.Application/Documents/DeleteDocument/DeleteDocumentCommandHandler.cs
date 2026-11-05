using AutoElite.Application.Chats;

namespace AutoElite.Application.Documents.DeleteDocument;

/// <summary>
/// Removes the record; <see cref="DeleteFileWhenDocumentDeletedHandler"/> removes the file afterwards. A document sent
/// in a chat message is deleted only with its message.
/// </summary>
internal sealed class DeleteDocumentCommandHandler(
    DocumentAccess documentAccess,
    IDocumentRepository documentRepository,
    IChatRepository chatRepository,
    TimeProvider timeProvider) : ICommandHandler<DeleteDocumentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var documentResult = await documentAccess.LoadForChangeAsync(command.DocumentId, cancellationToken);
        if (documentResult.IsFailure)
            return documentResult;

        var document = documentResult.Value;
        if (document.Owner.Kind == EntityKind.Chat && await chatRepository.IsAnyAttachedAsync([document.Id], cancellationToken))
            return Result.Failure(ApplicationErrors.Document.SentInMessage);

        var deleteResult = document.Delete(timeProvider.GetUtcNow().UtcDateTime);
        if (deleteResult.IsFailure)
            return deleteResult;

        documentRepository.Remove(document);

        return Result.Success();
    }
}
