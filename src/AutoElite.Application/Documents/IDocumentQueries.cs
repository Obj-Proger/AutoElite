namespace AutoElite.Application.Documents;

/// <summary>Reads documents for queries.</summary>
public interface IDocumentQueries
{
    /// <summary>Reads the documents attached to a record, the newest first.</summary>
    /// <param name="owner">The record.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DocumentItem>> GetByOwnerAsync(EntityReference owner, CancellationToken cancellationToken = default);
}
