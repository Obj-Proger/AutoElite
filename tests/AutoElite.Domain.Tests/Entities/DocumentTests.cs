namespace AutoElite.Domain.Tests.Entities;

public class DocumentTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _uploaderId = new("11111111-1111-1111-1111-111111111111");
    private static readonly EntityReference _contract = EntityReference.Create(EntityKind.Contract, new("55555555-5555-5555-5555-555555555555")).Value;

    private static readonly string[] _expectedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp", ".docx", ".xlsx", ".pptx", ".txt"];

    private const string StorageKey = "documents/2026/04/0193a1b2.pdf";

    private static Result<Document> Attach(string? fileName = "passport.pdf", string? contentType = "application/pdf", long sizeBytes = 1024) =>
        Document.Attach(_contract, fileName, contentType, sizeBytes, StorageKey, null, _uploaderId, _now);

    [Fact]
    public void Attach_ValidFile_ReturnsDocumentWithGivenValues()
    {
        var result = Document.Attach(_contract, "  passport.pdf  ", " Application/PDF ", 2048, $"  {StorageKey}  ", "  Passport, page 2  ", _uploaderId, _now);

        result.IsSuccess.Should().BeTrue();
        var document = result.Value;
        document.Owner.Should().Be(_contract);
        document.FileName.Should().Be("passport.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.SizeBytes.Should().Be(2048);
        document.StorageKey.Should().Be(StorageKey);
        document.Description.Should().Be("Passport, page 2");
        document.UploadedByUserId.Should().Be(_uploaderId);
        document.UploadedAtUtc.Should().Be(_now);
        document.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("scan.JPG", "image/jpeg")]
    [InlineData("scan.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("notes.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("table.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("readme.txt", "text/plain")]
    public void Attach_AllowedFormat_ReturnsDocument(string fileName, string contentType)
    {
        var result = Attach(fileName, contentType);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("virus.exe", "application/octet-stream")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("no-extension", "application/pdf")]
    public void Attach_FormatNotAllowed_ReturnsFormatNotAllowedError(string fileName, string contentType)
    {
        var result = Attach(fileName, contentType);

        result.Error.Should().Be(DomainErrors.Document.FormatNotAllowed);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData(null)]
    public void Attach_ContentTypeNotMatchingExtension_ReturnsContentTypeMismatchError(string? contentType)
    {
        var result = Attach("passport.pdf", contentType);

        result.Error.Should().Be(DomainErrors.Document.ContentTypeMismatch);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Attach_EmptyFileName_ReturnsFileNameEmptyError(string? fileName)
    {
        var result = Attach(fileName);

        result.Error.Should().Be(DomainErrors.Document.FileNameEmpty);
    }

    [Theory]
    [InlineData("../secret.pdf")]
    [InlineData("C:\\docs\\passport.pdf")]
    [InlineData("pass\u0000port.pdf")]
    public void Attach_FileNameWithPathOrControlCharacters_ReturnsFileNameInvalidError(string fileName)
    {
        var result = Attach(fileName);

        result.Error.Should().Be(DomainErrors.Document.FileNameInvalid);
    }

    [Fact]
    public void Attach_FileNameTooLong_ReturnsFileNameTooLongError()
    {
        var result = Attach(new string('a', 252) + ".pdf");

        result.Error.Should().Be(DomainErrors.Document.FileNameTooLong);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(Document.MaxSizeBytes)]
    public void Attach_SizeWithinLimit_ReturnsDocument(long sizeBytes)
    {
        var result = Attach(sizeBytes: sizeBytes);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(Document.MaxSizeBytes + 1)]
    public void Attach_SizeOutOfRange_ReturnsSizeOutOfRangeError(long sizeBytes)
    {
        var result = Attach(sizeBytes: sizeBytes);

        result.Error.Should().Be(DomainErrors.Document.SizeOutOfRange);
    }

    [Fact]
    public void Attach_EmptyStorageKey_ReturnsStorageKeyEmptyError()
    {
        var result = Document.Attach(_contract, "passport.pdf", "application/pdf", 1024, " ", null, _uploaderId, _now);

        result.Error.Should().Be(DomainErrors.Document.StorageKeyEmpty);
    }

    [Fact]
    public void Attach_EmptyUploader_ReturnsUploadedByUserIdEmptyError()
    {
        var result = Document.Attach(_contract, "passport.pdf", "application/pdf", 1024, StorageKey, null, Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.Document.UploadedByUserIdEmpty);
    }

    [Fact]
    public void Attach_DescriptionTooLong_ReturnsDescriptionTooLongError()
    {
        var result = Document.Attach(_contract, "passport.pdf", "application/pdf", 1024, StorageKey, new string('a', 501), _uploaderId, _now);

        result.Error.Should().Be(DomainErrors.Document.DescriptionTooLong);
    }

    [Fact]
    public void AllowedExtensions_ListsTheSupportedFormats()
    {
        Document.AllowedExtensions.Should().BeEquivalentTo(_expectedExtensions);
    }

    [Fact]
    public void UpdateDescription_ChangesDescription()
    {
        var document = Attach().Value;

        var result = document.UpdateDescription("Medical certificate");

        result.IsSuccess.Should().BeTrue();
        document.Description.Should().Be("Medical certificate");
    }

    [Fact]
    public void Delete_Document_MarksDeletedAndRaisesDeletedEventWithStorageKey()
    {
        var document = Attach().Value;

        var result = document.Delete(_now);

        result.IsSuccess.Should().BeTrue();
        document.IsDeleted.Should().BeTrue();
        document.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new DocumentDeletedDomainEvent(document.Id, EntityKind.Contract, _contract.Id, StorageKey, _now));
    }

    [Fact]
    public void Delete_DeletedDocument_ReturnsAlreadyDeletedError()
    {
        var document = Attach().Value;
        document.Delete(_now);

        var result = document.Delete(_now);

        result.Error.Should().Be(DomainErrors.Document.AlreadyDeleted);
        document.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdateDescription_DeletedDocument_ReturnsAlreadyDeletedError()
    {
        var document = Attach().Value;
        document.Delete(_now);

        var result = document.UpdateDescription("Too late");

        result.Error.Should().Be(DomainErrors.Document.AlreadyDeleted);
    }
}
