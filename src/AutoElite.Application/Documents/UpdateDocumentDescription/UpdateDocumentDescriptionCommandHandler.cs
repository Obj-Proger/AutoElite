namespace AutoElite.Application.Documents.UpdateDocumentDescription;

/// <summary>Applies the new description.</summary>
internal sealed class UpdateDocumentDescriptionCommandHandler(DocumentAccess documentAccess)
    : ICommandHandler<UpdateDocumentDescriptionCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDocumentDescriptionCommand command, CancellationToken cancellationToken)
    {
        var documentResult = await documentAccess.LoadForChangeAsync(command.DocumentId, cancellationToken);

        return documentResult.IsFailure ? documentResult : documentResult.Value.UpdateDescription(command.Description);
    }
}
