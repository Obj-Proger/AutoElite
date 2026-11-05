namespace AutoElite.Application.Documents.GetDocumentFile;

/// <summary>Checks access and opens the stored file.</summary>
internal sealed class GetDocumentFileQueryHandler(DocumentAccess documentAccess, IFileStorage fileStorage)
    : IQueryHandler<GetDocumentFileQuery, DocumentFile>
{
    /// <inheritdoc />
    public async Task<Result<DocumentFile>> Handle(GetDocumentFileQuery query, CancellationToken cancellationToken)
    {
        var documentResult = await documentAccess.LoadForReadingAsync(query.DocumentId, cancellationToken);
        if (documentResult.IsFailure)
            return Result.Failure<DocumentFile>(documentResult.Error);

        var document = documentResult.Value;
        var content = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);

        return content is null
            ? Result.Failure<DocumentFile>(ApplicationErrors.NotFound<Document>())
            : new DocumentFile(document.FileName, document.ContentType, document.SizeBytes, content);
    }
}
