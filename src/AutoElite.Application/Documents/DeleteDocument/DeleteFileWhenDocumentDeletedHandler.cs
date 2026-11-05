using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Documents.DeleteDocument;

/// <summary>Removes the file of a deleted document from storage, once the deletion of the record is committed.</summary>
/// <remarks>
/// Unlike other event handlers, this one acts outside the database. That is safe because deleting a file is
/// idempotent: if the event is delivered again, the file is already gone and nothing happens.
/// </remarks>
internal sealed class DeleteFileWhenDocumentDeletedHandler(IFileStorage fileStorage)
    : IDomainEventHandler<DocumentDeletedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(DocumentDeletedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        fileStorage.DeleteAsync(domainEvent.StorageKey, cancellationToken);
}
