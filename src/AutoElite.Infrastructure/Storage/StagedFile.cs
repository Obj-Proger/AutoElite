namespace AutoElite.Infrastructure.Storage;

/// <summary>
/// A file received into a temporary file of the storage and checked there before it is kept: read once as a stream,
/// it must be exactly the declared size and of the declared type. Disposing it deletes the temporary file unless it
/// was moved away.
/// </summary>
internal sealed class StagedFile : IAsyncDisposable
{
    private const int BufferSize = 81920;

    private bool _moved;

    private StagedFile(string path, bool isValid)
    {
        Path = path;
        IsValid = isValid;
    }

    /// <summary>Gets the path of the temporary file.</summary>
    public string Path { get; }

    /// <summary>Gets a value indicating whether the content is of the declared size and type.</summary>
    public bool IsValid { get; }

    /// <summary>
    /// Receives the content into a new temporary file in the folder, stopping as soon as it is longer than declared.
    /// </summary>
    /// <param name="folder">The folder of temporary files, on the same disk as the kept files.</param>
    /// <param name="content">The content, read once from its current position.</param>
    /// <param name="contentType">The declared content type.</param>
    /// <param name="sizeBytes">The declared size.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public static async Task<StagedFile> ReceiveAsync(
        string folder, Stream content, string contentType, long sizeBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        Directory.CreateDirectory(folder);
        var path = System.IO.Path.Combine(folder, $"{Guid.NewGuid():N}.part");
        var header = new byte[FileSignatures.HeaderLength];
        var headerLength = 0;
        var textCheck = FileSignatures.IsText(contentType) ? FileSignatures.CreateTextCheck() : null;
        long received = 0;
        try
        {
            await using (var target = LocalFiles.CreateNew(path))
            {
                var buffer = new byte[BufferSize];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    received += read;
                    if (received > sizeBytes)
                        break;

                    var piece = buffer.AsMemory(0, read);
                    var toHeader = Math.Min(read, header.Length - headerLength);
                    piece.Span[..toHeader].CopyTo(header.AsSpan(headerLength));
                    headerLength += toHeader;
                    textCheck?.Feed(piece.Span, isLast: false);
                    await target.WriteAsync(piece, cancellationToken);
                }
            }

            textCheck?.Feed([], isLast: true);
            var isValid = received == sizeBytes
                && FileSignatures.Matches(contentType, header.AsSpan(0, headerLength))
                && textCheck?.IsValid != false;

            return new StagedFile(path, isValid);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    /// <summary>Moves the file to its place, replacing nothing; afterwards disposing leaves it there.</summary>
    /// <param name="destination">The path the file is kept at.</param>
    public void MoveTo(string destination)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
        File.Move(Path, destination, overwrite: false);
        _moved = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_moved)
            File.Delete(Path);

        return ValueTask.CompletedTask;
    }
}
