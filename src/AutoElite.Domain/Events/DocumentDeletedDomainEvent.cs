using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>Raised when a document is deleted; its file is removed from storage after the transaction commits.</summary>
/// <param name="DocumentId">The identifier of the document.</param>
/// <param name="OwnerKind">The kind of the object the document was attached to.</param>
/// <param name="OwnerId">The identifier of that object.</param>
/// <param name="StorageKey">The key of the file in file storage.</param>
/// <param name="OccurredOnUtc">The UTC time of the deletion.</param>
public sealed record DocumentDeletedDomainEvent(
    Guid DocumentId, EntityKind OwnerKind, Guid OwnerId, string StorageKey, DateTime OccurredOnUtc) : IDomainEvent;
