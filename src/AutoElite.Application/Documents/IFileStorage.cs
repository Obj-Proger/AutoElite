namespace AutoElite.Application.Documents;

/// <summary>Keeps the files of documents (O-08); the infrastructure decides where.</summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores a file under the key. The content must be exactly <paramref name="sizeBytes"/> long and its signature
    /// must match the content type, since both are declared by the client; otherwise nothing is stored.
    /// </summary>
    /// <param name="storageKey">The key to store the file under.</param>
    /// <param name="content">The content, read once from its current position.</param>
    /// <param name="contentType">The content type the domain accepted for the file name.</param>
    /// <param name="sizeBytes">The declared size.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>Success, or <see cref="ApplicationErrors.Document.ContentMismatch"/>.</returns>
    Task<Result> SaveAsync(
        string storageKey, Stream content, string contentType, long sizeBytes, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored file for reading; the caller disposes the stream.</summary>
    /// <param name="storageKey">The key of the file.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The stream, or <see langword="null"/> if no file is stored under the key.</returns>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored file; deleting a missing file does nothing, so a repeated call is harmless.</summary>
    /// <param name="storageKey">The key of the file.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
