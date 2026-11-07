using AutoElite.Application.Documents;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads the documents of a record with the names of who attached them, without tracking.</summary>
internal sealed class DocumentQueries(AutoEliteDbContext dbContext) : IDocumentQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentItem>> GetByOwnerAsync(EntityReference owner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var (kind, id) = (owner.Kind, owner.Id);
        var found = await (
                from document in dbContext.Set<Document>().AsNoTracking()
                join uploader in dbContext.Set<User>() on document.UploadedByUserId equals uploader.Id
                where document.Owner.Id == id && document.Owner.Kind == kind && !document.IsDeleted
                orderby document.UploadedAtUtc descending, document.Id descending
                select new
                {
                    document.Id,
                    document.FileName,
                    document.ContentType,
                    document.SizeBytes,
                    document.Description,
                    document.UploadedByUserId,
                    uploader.FullName.LastName,
                    uploader.FullName.FirstName,
                    uploader.FullName.MiddleName,
                    document.UploadedAtUtc
                })
            .ToListAsync(cancellationToken);

        return
        [
            .. found.Select(row => new DocumentItem(
                row.Id, row.FileName, row.ContentType, row.SizeBytes, row.Description, row.UploadedByUserId,
                PersonNames.Format(row.LastName, row.FirstName, row.MiddleName)!, row.UploadedAtUtc))
        ];
    }
}
