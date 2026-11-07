namespace AutoElite.Infrastructure.Import;

/// <summary>
/// Reads a stream that must not be longer than it declares, such as an entry of an archive, whose real content a crafted
/// archive can make far longer than its header says: reading past the declared length throws instead of going on.
/// Disposing it disposes the stream it reads.
/// </summary>
/// <param name="inner">The stream read.</param>
/// <param name="length">The declared length.</param>
internal sealed class BoundedReadStream(Stream inner, long length) : Stream
{
    private long _read;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => length;

    /// <inheritdoc />
    public override long Position
    {
        get => _read;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
            return 0;

        var remaining = length - _read;
        if (remaining == 0)
        {
            Span<byte> probe = stackalloc byte[1];

            return inner.Read(probe) == 0 ? 0 : throw TooLong();
        }

        var read = inner.Read(buffer[..(int)Math.Min(buffer.Length, remaining)]);
        _read += read;

        return read;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty)
            return 0;

        var remaining = length - _read;
        if (remaining == 0)
            return await inner.ReadAsync(new byte[1], cancellationToken) == 0 ? 0 : throw TooLong();

        var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, remaining)], cancellationToken);
        _read += read;

        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();

        base.Dispose(disposing);
    }

    private static InvalidDataException TooLong() =>
        new("A file in the archive is longer than the archive declares, so the archive is damaged.");
}
