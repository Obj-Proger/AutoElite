namespace AutoElite.Application.Documents.GetDocumentFile;

/// <summary>Opens the file of a document for download, if the caller may read the documents of its owner.</summary>
/// <param name="DocumentId">The identifier of the document.</param>
public sealed record GetDocumentFileQuery(Guid DocumentId) : IQuery<DocumentFile>;
