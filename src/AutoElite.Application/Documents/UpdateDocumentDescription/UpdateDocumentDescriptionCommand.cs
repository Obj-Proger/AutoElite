namespace AutoElite.Application.Documents.UpdateDocumentDescription;

/// <summary>Changes what a document is described as: by whoever attached it or manages the owner's documents.</summary>
/// <param name="DocumentId">The identifier of the document.</param>
/// <param name="Description">The new description, or <see langword="null"/> to clear it.</param>
public sealed record UpdateDocumentDescriptionCommand(Guid DocumentId, string? Description) : ICommand;
