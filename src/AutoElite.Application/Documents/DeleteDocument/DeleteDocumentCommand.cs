namespace AutoElite.Application.Documents.DeleteDocument;

/// <summary>
/// Deletes a document together with its file (FR-13.2): by whoever attached it or manages the owner's documents. The
/// file is removed after the deletion is saved. An attachment of a chat message is deleted by deleting the message.
/// </summary>
/// <param name="DocumentId">The identifier of the document.</param>
public sealed record DeleteDocumentCommand(Guid DocumentId) : ICommand;
