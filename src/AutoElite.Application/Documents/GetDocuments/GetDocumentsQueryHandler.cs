namespace AutoElite.Application.Documents.GetDocuments;

/// <summary>Reads the list if the caller may read the record's documents.</summary>
internal sealed class GetDocumentsQueryHandler(IDocumentQueries documentQueries, DocumentOwners owners)
    : IQueryHandler<GetDocumentsQuery, IReadOnlyList<DocumentItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentItem>>> Handle(GetDocumentsQuery query, CancellationToken cancellationToken)
    {
        var ownerResult = EntityReference.Create(query.OwnerKind, query.OwnerId);
        if (ownerResult.IsFailure)
            return Result.Failure<IReadOnlyList<DocumentItem>>(ownerResult.Error);

        if (await owners.GetAccessAsync(ownerResult.Value, cancellationToken) == DocumentAccessLevel.None)
            return Result.Failure<IReadOnlyList<DocumentItem>>(ApplicationErrors.Document.OwnerNotFound);

        return Result.Success(await documentQueries.GetByOwnerAsync(ownerResult.Value, cancellationToken));
    }
}
