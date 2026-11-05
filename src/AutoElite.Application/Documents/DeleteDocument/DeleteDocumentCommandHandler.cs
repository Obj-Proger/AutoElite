namespace AutoElite.Application.Documents.DeleteDocument;

/// <summary>Removes the record; <see cref="DeleteFileWhenDocumentDeletedHandler"/> removes the file afterwards.</summary>
internal sealed class DeleteDocumentCommandHandler(
    DocumentAccess documentAccess,
    IDocumentRepository documentRepository,
    TimeProvider timeProvider) : ICommandHandler<DeleteDocumentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var documentResult = await documentAccess.LoadForChangeAsync(command.DocumentId, cancellationToken);
        if (documentResult.IsFailure)
            return documentResult;

        var document = documentResult.Value;
        var deleteResult = document.Delete(timeProvider.GetUtcNow().UtcDateTime);
        if (deleteResult.IsFailure)
            return deleteResult;

        documentRepository.Remove(document);

        return Result.Success();
    }
}
