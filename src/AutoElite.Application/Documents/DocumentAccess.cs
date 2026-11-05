namespace AutoElite.Application.Documents;

/// <summary>Loads a document for an action of the caller, checking the caller's access to its owner.</summary>
internal sealed class DocumentAccess(IDocumentRepository documentRepository, DocumentOwners owners, ICurrentUser currentUser)
{
    /// <summary>Loads the document if the caller may read the documents of its owner.</summary>
    /// <param name="documentId">The identifier of the document.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Document>> LoadForReadingAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        return document is not null && await owners.GetAccessAsync(document.Owner, cancellationToken) >= DocumentAccessLevel.Read
            ? document
            : Result.Failure<Document>(ApplicationErrors.NotFound<Document>());
    }

    /// <summary>
    /// Loads the document for a change: the caller may manage the owner's documents, or contribute to them and
    /// attached this one. A caller who may only read gets Forbidden.
    /// </summary>
    /// <param name="documentId">The identifier of the document.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Document>> LoadForChangeAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        var access = document is null ? DocumentAccessLevel.None : await owners.GetAccessAsync(document.Owner, cancellationToken);
        if (document is null || access == DocumentAccessLevel.None)
            return Result.Failure<Document>(ApplicationErrors.NotFound<Document>());

        var mayChange = access == DocumentAccessLevel.Manage
            || (access == DocumentAccessLevel.Contribute && document.UploadedByUserId == currentUser.UserId);

        return mayChange ? document : Result.Failure<Document>(ApplicationErrors.Auth.Forbidden);
    }
}
