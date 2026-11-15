using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The documents of a student: their own, such as a scan of their passport or a medical certificate, which they add
/// and delete, and those the school attached to their contracts, which they read. The lists are read from the API or,
/// without a network, from the device; adding and deleting need the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class StudentDocuments(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads the documents of a record, the latest first.</summary>
    /// <param name="kind">The kind of the record: the user or a contract.</param>
    /// <param name="ownerId">The record.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The documents.</returns>
    public Task<Cached<IReadOnlyList<DocumentItem>>> ReadAsync(EntityKind kind, Guid ownerId, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            string.Create(CultureInfo.InvariantCulture, $"documents/{kind}/{ownerId:N}"),
            token => createApi().Api.V1.Documents.GetAsync(
                request =>
                {
                    request.QueryParameters.OwnerKind = kind;
                    request.QueryParameters.OwnerId = ownerId;
                },
                token),
            DocumentItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Adds a document of the user, up to 20 MB, which the API checks to match its type.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="file">The file the user chose.</param>
    /// <param name="description">What the document is, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the document is added.</returns>
    public async Task AddAsync(Guid userId, FileResult file, string? description, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        await using var content = await file.OpenReadAsync();
        var form = new MultipartBody();
        var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;
        form.AddOrReplacePart("file", contentType, content, file.FileName);
        form.AddOrReplacePart("ownerKind", "text/plain", nameof(EntityKind.User));
        form.AddOrReplacePart("ownerId", "text/plain", userId.ToString("D", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(description))
            form.AddOrReplacePart("description", "text/plain", description.Trim());

        await createApi().AttachDocumentAsync(form, cancellationToken);
    }

    /// <summary>Deletes a document the user added.</summary>
    /// <param name="documentId">The document.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the document is deleted.</returns>
    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Documents[documentId].DeleteAsync(cancellationToken: cancellationToken);
}
