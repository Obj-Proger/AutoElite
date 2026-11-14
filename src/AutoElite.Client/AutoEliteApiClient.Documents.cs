using AutoElite.Client.Api.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace AutoElite.Client.Api;

/// <summary>
/// Attaching a file. Kiota describes the multipart form of POST /api/v1/documents as a model of its fields, which its
/// multipart serializer cannot write, so the form is sent here as a <see cref="MultipartBody"/> of parts through the
/// same request adapter, with the same errors as the generated requests.
/// </summary>
public partial class AutoEliteApiClient
{
    private const string DocumentsTemplate = "{+baseurl}/api/v1/documents";

    /// <summary>
    /// Attaches a file of up to 20 MB to a record: the form has the part <c>file</c> with the file and its name, and the
    /// text parts <c>ownerKind</c>, <c>ownerId</c> and, if wanted, <c>description</c>.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The identifier of the new document.</returns>
    /// <exception cref="ProblemDetails">The API refused the file.</exception>
    public async Task<CreatedResource> AttachDocumentAsync(MultipartBody form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        var request = new RequestInformation(
            Method.POST, DocumentsTemplate, new Dictionary<string, object> { ["baseurl"] = RequestAdapter.BaseUrl ?? string.Empty });
        request.Headers.TryAdd("Accept", "application/json");
        request.SetContentFromParsable(RequestAdapter, "multipart/form-data", form);
        var errors = new Dictionary<string, ParsableFactory<IParsable>> { ["XXX"] = ProblemDetails.CreateFromDiscriminatorValue };

        return await RequestAdapter.SendAsync(request, CreatedResource.CreateFromDiscriminatorValue, errors, cancellationToken)
            ?? throw new InvalidOperationException("The API answered without the document.");
    }
}
