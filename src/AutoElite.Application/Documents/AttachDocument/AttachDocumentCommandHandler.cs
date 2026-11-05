namespace AutoElite.Application.Documents.AttachDocument;

/// <summary>Checks the caller's access and the file, stores the file and records the document.</summary>
/// <remarks>
/// The document is validated before the file is stored, so an invalid request stores nothing. If saving the record
/// fails after the file was stored, the file is left without a record; the infrastructure sweeps such files away.
/// </remarks>
internal sealed class AttachDocumentCommandHandler(
    IDocumentRepository documentRepository,
    DocumentOwners owners,
    IFileStorage fileStorage,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<AttachDocumentCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AttachDocumentCommand command, CancellationToken cancellationToken)
    {
        if (!DocumentOwners.SupportedKinds.Contains(command.OwnerKind))
            return Result.Failure<Guid>(ApplicationErrors.Document.OwnerKindNotSupported);

        var ownerResult = EntityReference.Create(command.OwnerKind, command.OwnerId);
        if (ownerResult.IsFailure)
            return Result.Failure<Guid>(ownerResult.Error);

        var access = await owners.GetAccessAsync(ownerResult.Value, cancellationToken);
        if (access == DocumentAccessLevel.None)
            return Result.Failure<Guid>(ApplicationErrors.Document.OwnerNotFound);

        if (access < DocumentAccessLevel.Contribute)
            return Result.Failure<Guid>(ApplicationErrors.Auth.Forbidden);

        var documentResult = Document.Attach(
            ownerResult.Value,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            $"documents/{Guid.CreateVersion7():N}",
            command.Description,
            currentUser.GetRequiredUserId(),
            timeProvider.GetUtcNow().UtcDateTime);
        if (documentResult.IsFailure)
            return Result.Failure<Guid>(documentResult.Error);

        var document = documentResult.Value;
        var saveResult = await fileStorage.SaveAsync(
            document.StorageKey, command.Content!, document.ContentType, document.SizeBytes, cancellationToken);
        if (saveResult.IsFailure)
            return Result.Failure<Guid>(saveResult.Error);

        documentRepository.Add(document);

        return document.Id;
    }
}
