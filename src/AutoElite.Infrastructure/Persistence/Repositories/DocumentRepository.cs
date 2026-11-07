using AutoElite.Application.Documents;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>
/// Loads, adds and removes documents. Removing a document empties the references of the records that showed it as
/// a photo or an image (<c>ON DELETE SET NULL</c>), so that the record never points at a missing file.
/// </summary>
internal sealed class DocumentRepository(AutoEliteDbContext dbContext) : Repository<Document>(dbContext), IDocumentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Document>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        List<Guid> wanted = [.. ids];

        return await Aggregates.Where(document => wanted.Contains(document.Id)).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(Document document) => Aggregates.Remove(document);
}
