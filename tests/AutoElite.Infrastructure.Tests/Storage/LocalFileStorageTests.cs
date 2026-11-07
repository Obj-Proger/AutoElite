using System.Text;
using AutoElite.Application.Common;
using AutoElite.Domain.Common;
using AutoElite.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private const string Png = "image/png";

    private const string Text = "text/plain";

    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"autoelite-files-{Guid.NewGuid():N}");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string NewKey() => $"documents/{Guid.NewGuid():N}";

    private LocalFileStorage Storage() =>
        new(Options.Create(new FileStorageOptions { RootPath = _root }));

    private Task<Result> SaveAsync(string key, byte[] content, string contentType, long? sizeBytes = null) =>
        Storage().SaveAsync(key, new MemoryStream(content), contentType, sizeBytes ?? content.Length, CancellationToken);

    private string[] Incoming() =>
        Directory.Exists(Path.Combine(_root, LocalFiles.IncomingFolder))
            ? Directory.GetFiles(Path.Combine(_root, LocalFiles.IncomingFolder))
            : [];

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheSameContent()
    {
        var key = NewKey();

        var saved = await SaveAsync(key, _png, Png);
        await using var stream = await Storage().OpenReadAsync(key, CancellationToken);
        using var copy = new MemoryStream();
        await stream!.CopyToAsync(copy, CancellationToken);

        saved.IsSuccess.Should().BeTrue();
        copy.ToArray().Should().Equal(_png);
        Incoming().Should().BeEmpty();
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public async Task Save_ContentOfAnotherSize_KeepsNothing(long declaredSize)
    {
        var key = NewKey();

        var saved = await SaveAsync(key, _png, Png, declaredSize);

        saved.Error.Should().Be(ApplicationErrors.Document.ContentMismatch);
        (await Storage().OpenReadAsync(key, CancellationToken)).Should().BeNull();
        Incoming().Should().BeEmpty();
    }

    [Fact]
    public async Task Save_ContentOfAnotherType_KeepsNothing()
    {
        var key = NewKey();

        var saved = await SaveAsync(key, _png, "application/pdf");

        saved.Error.Should().Be(ApplicationErrors.Document.ContentMismatch);
        (await Storage().OpenReadAsync(key, CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Save_LongUtf8Text_IsAccepted_ThoughCharactersSpanTheReadBuffers()
    {
        var text = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("Ёлка ПДД 🚗 ", 20_000)));

        var saved = await SaveAsync(NewKey(), text, Text);

        saved.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(new byte[] { 0x41, 0x00, 0x42 })]
    [InlineData(new byte[] { 0x41, 0xC3, 0x28 })]
    [InlineData(new byte[] { 0x41, 0xD0 })]
    public async Task Save_TextThatIsNotUtf8OrHasNul_KeepsNothing(byte[] content)
    {
        var saved = await SaveAsync(NewKey(), content, Text);

        saved.Error.Should().Be(ApplicationErrors.Document.ContentMismatch);
    }

    [Theory]
    [InlineData("documents/../../etc/passwd")]
    [InlineData("documents/ABCDEF0123456789ABCDEF0123456789")]
    [InlineData("other/0123456789abcdef0123456789abcdef")]
    [InlineData("documents/0123456789abcdef0123456789abcdef/x")]
    public async Task AnyOperation_KeyTheApplicationDoesNotMake_Throws(string key)
    {
        var save = () => SaveAsync(key, _png, Png);
        var open = () => Storage().OpenReadAsync(key, CancellationToken);
        var delete = () => Storage().DeleteAsync(key, CancellationToken);

        await save.Should().ThrowAsync<ArgumentException>();
        await open.Should().ThrowAsync<ArgumentException>();
        await delete.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Delete_StoredAndMissingFiles_RemovesTheStoredOneAndIgnoresTheMissing()
    {
        var key = NewKey();
        await SaveAsync(key, _png, Png);

        await Storage().DeleteAsync(key, CancellationToken);
        await Storage().DeleteAsync(key, CancellationToken);

        (await Storage().OpenReadAsync(key, CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Save_SecondFileUnderAKey_IsRefused()
    {
        var key = NewKey();
        await SaveAsync(key, _png, Png);

        var again = () => SaveAsync(key, _png, Png);

        await again.Should().ThrowAsync<IOException>();
        Incoming().Should().BeEmpty();
    }

    [Theory]
    [InlineData("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 }, true)]
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData("image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, true)]
    [InlineData("image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45 }, false)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new byte[] { 0x50, 0x4B, 3, 4 }, true)]
    [InlineData("image/jpeg", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, false)]
    [InlineData("application/x-msdownload", new byte[] { 0x4D, 0x5A }, false)]
    public void Signatures_TellTheTypeByTheFirstBytes(string contentType, byte[] header, bool matches)
    {
        FileSignatures.Matches(contentType, header).Should().Be(matches);
    }

    [Fact]
    public void Options_RelativeOrApplicationFolder_AreRefused()
    {
        new FileStorageOptions { RootPath = "files" }.Problems().Should().ContainSingle();
        new FileStorageOptions { RootPath = Path.Combine(AppContext.BaseDirectory, "files") }.Problems().Should().ContainSingle();
        new FileStorageOptions { RootPath = _root }.Problems().Should().BeEmpty();
    }
}
