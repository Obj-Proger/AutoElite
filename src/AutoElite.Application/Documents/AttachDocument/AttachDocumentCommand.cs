namespace AutoElite.Application.Documents.AttachDocument;

/// <summary>
/// Attaches a file to a record (FR-13.1): a scan to a contract, a certificate to a user, a material to a lesson and the
/// like. <see cref="DocumentOwners"/> decides who may attach to which records.
/// </summary>
/// <param name="OwnerKind">The kind of the record.</param>
/// <param name="OwnerId">The identifier of the record.</param>
/// <param name="FileName">The original file name, without a path.</param>
/// <param name="ContentType">The declared content type; it must match the extension and the content.</param>
/// <param name="SizeBytes">The size of the content, up to 20 MB.</param>
/// <param name="Description">What the document is, if given, for example "Passport, page 2".</param>
/// <param name="Content">The content, read once; the sender disposes it.</param>
public sealed record AttachDocumentCommand(
    EntityKind OwnerKind,
    Guid OwnerId,
    string? FileName,
    string? ContentType,
    long SizeBytes,
    string? Description,
    Stream? Content) : ICommand<Guid>;
