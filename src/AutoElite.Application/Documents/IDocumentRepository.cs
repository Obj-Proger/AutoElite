namespace AutoElite.Application.Documents;

/// <summary>Loads, adds and removes documents for commands.</summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>Loads the documents with the identifiers in one query; unknown ones are skipped.</summary>
    /// <param name="ids">The identifiers.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<Document>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Removes a document; it is deleted on the next save.</summary>
    /// <param name="document">The document to remove.</param>
    void Remove(Document document);
}
