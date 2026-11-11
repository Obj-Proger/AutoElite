using AutoElite.API.Common;
using AutoElite.Application.Documents;
using AutoElite.Application.Documents.AttachDocument;
using AutoElite.Application.Documents.DeleteDocument;
using AutoElite.Application.Documents.GetDocumentFile;
using AutoElite.Application.Documents.GetDocuments;
using AutoElite.Application.Documents.UpdateDocumentDescription;
using AutoElite.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The files attached to records: scans of contracts, certificates of users, materials of lessons, illustrations of
/// questions and the like. Who may read, attach and delete depends on the record; a profile photo and the
/// illustration of a question are read by everyone signed in.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/documents")]
public sealed class DocumentsController(IDispatcher dispatcher) : ApiControllerBase
{
    // The largest document and the rest of the form around it.
    private const long MaxUploadRequestBytes = Document.MaxSizeBytes + 1024 * 1024;

    /// <summary>Lists the documents attached to a record, the newest first.</summary>
    /// <param name="ownerKind">The kind of the record.</param>
    /// <param name="ownerId">The record.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The documents.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetDocuments(EntityKind ownerKind, Guid ownerId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetDocumentsQuery(ownerKind, ownerId), cancellationToken));

    /// <summary>
    /// Downloads the file of a document as an attachment with its original name; browsers are told not to guess
    /// another type of content.
    /// </summary>
    /// <param name="documentId">The document.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The content of the file.</returns>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetDocumentFileQuery(documentId), cancellationToken);
        if (result.IsFailure)
            return Failure(result.Error);

        var file = result.Value;
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.ContentLength = file.SizeBytes;

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Attaches a file of up to 20 MB to a record; its type must match its extension and its content. The Location
    /// header leads to the file.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="ownerKind">The kind of the record.</param>
    /// <param name="ownerId">The record.</param>
    /// <param name="description">What the document is, for example "Passport, page 2".</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the document.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Attach(
        IFormFile? file,
        [FromForm] EntityKind ownerKind,
        [FromForm] Guid ownerId,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();
        var command = new AttachDocumentCommand(
            ownerKind, ownerId, file?.FileName, file?.ContentType, file?.Length ?? 0, description, content);

        return RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(Download), "documentId");
    }

    /// <summary>Changes the description of a document.</summary>
    /// <param name="documentId">The document; it takes the place of the one in the body.</param>
    /// <param name="command">The description; empty clears it.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{documentId:guid}/description")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateDescription(
        Guid documentId, UpdateDocumentDescriptionCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { DocumentId = documentId }, cancellationToken));

    /// <summary>Deletes a document together with its file; a chat attachment goes with its message instead.</summary>
    /// <param name="documentId">The document.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeleteDocumentCommand(documentId), cancellationToken));
}
