namespace AutoElite.Application.Documents.GetDocuments;

/// <summary>Lists the documents attached to a record, the newest first (FR-13.2).</summary>
/// <param name="OwnerKind">The kind of the record.</param>
/// <param name="OwnerId">The identifier of the record.</param>
public sealed record GetDocumentsQuery(EntityKind OwnerKind, Guid OwnerId) : IQuery<IReadOnlyList<DocumentItem>>;
