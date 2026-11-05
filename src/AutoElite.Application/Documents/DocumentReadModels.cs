namespace AutoElite.Application.Documents;

/// <summary>A document in the list of its owner.</summary>
/// <param name="Id">The identifier of the document.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="SizeBytes">The size of the file in bytes.</param>
/// <param name="Description">What the document is, if given.</param>
/// <param name="UploadedByUserId">The user who attached it.</param>
/// <param name="UploadedByName">The full name of that user.</param>
/// <param name="UploadedAtUtc">When it was attached.</param>
public sealed record DocumentItem(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Description,
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime UploadedAtUtc);

/// <summary>The file of a document, ready to be sent to the client.</summary>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="SizeBytes">The size of the file in bytes.</param>
/// <param name="Content">The content; the receiver disposes it.</param>
public sealed record DocumentFile(string FileName, string ContentType, long SizeBytes, Stream Content);
