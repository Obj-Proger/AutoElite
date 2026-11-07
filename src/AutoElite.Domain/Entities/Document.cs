using System.Collections.Frozen;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A file attached to another object (FR-13.1, FR-13.2): a scan for a contract, a photo for a user, learning material
/// for a lesson or a course topic, an attachment in a chat.
/// </summary>
/// <remarks>
/// <para>
/// Only the formats in <see cref="AllowedExtensions"/> are accepted, up to <see cref="MaxSizeBytes"/>, and the file
/// extension must match the declared content type. The persistence layer also checks the file signature, because
/// the declared type comes from the client.
/// </para>
/// <para>
/// The file itself lives in file storage under <see cref="StorageKey"/>. <see cref="Delete"/> raises
/// <see cref="DocumentDeletedDomainEvent"/> and the caller removes the record; the file is deleted after the
/// transaction commits, so a rolled-back deletion never loses the file.
/// </para>
/// </remarks>
public sealed class Document : AuditableEntity
{
    /// <summary>The largest file that can be attached: 20 MB.</summary>
    public const long MaxSizeBytes = 20L * 1024 * 1024;

    /// <summary>The longest file name.</summary>
    public const int MaxFileNameLength = 255;

    /// <summary>The longest storage key.</summary>
    public const int MaxStorageKeyLength = 1024;

    /// <summary>The longest description.</summary>
    public const int MaxDescriptionLength = 500;

    // Lower-case extension -> the content type it must be declared with.
    private static readonly FrozenDictionary<string, string> _contentTypeByExtension = new Dictionary<string, string>
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets the file extensions that can be attached, in lower case with the leading dot.</summary>
    public static IReadOnlyCollection<string> AllowedExtensions => _contentTypeByExtension.Keys;

    /// <summary>Gets the object the document is attached to.</summary>
    public EntityReference Owner { get; private set; } = null!;

    /// <summary>Gets the original file name, without any path.</summary>
    public string FileName { get; private set; } = null!;

    /// <summary>Gets the content type, in lower case.</summary>
    public string ContentType { get; private set; } = null!;

    /// <summary>Gets the size of the file in bytes.</summary>
    public long SizeBytes { get; private set; }

    /// <summary>Gets the key of the file in file storage.</summary>
    public string StorageKey { get; private set; } = null!;

    /// <summary>Gets what the document is, for example "Passport, page 2", if given.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the identifier of the user who attached the document.</summary>
    public Guid UploadedByUserId { get; private set; }

    /// <summary>Gets the UTC time the document was attached.</summary>
    public DateTime UploadedAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether deletion has been requested.</summary>
    public bool IsDeleted { get; private set; }

    private Document(Guid id) : base(id)
    {
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Document()
    {
    }

    /// <summary>Attaches a file that has been stored.</summary>
    /// <param name="owner">The object to attach the file to; checking that it exists is up to the caller.</param>
    /// <param name="fileName">The original file name; a path, if any, is rejected.</param>
    /// <param name="contentType">The declared content type; it must match the extension.</param>
    /// <param name="sizeBytes">The size, from 1 byte to <see cref="MaxSizeBytes"/>.</param>
    /// <param name="storageKey">The key the file was stored under.</param>
    /// <param name="description">What the document is, if given.</param>
    /// <param name="uploadedByUserId">The user attaching the document.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The document, or the first validation failure encountered.</returns>
    public static Result<Document> Attach(
        EntityReference owner,
        string? fileName,
        string? contentType,
        long sizeBytes,
        string? storageKey,
        string? description,
        Guid uploadedByUserId,
        DateTime utcNow)
    {
        var nameResult = ValidateFileName(fileName);
        if (nameResult.IsFailure)
            return Result.Failure<Document>(nameResult.Error);

        var typeResult = ValidateContentType(nameResult.Value, contentType);
        if (typeResult.IsFailure)
            return Result.Failure<Document>(typeResult.Error);

        if (sizeBytes is < 1 or > MaxSizeBytes)
            return Result.Failure<Document>(DomainErrors.Document.SizeOutOfRange);

        var keyResult = Guard.RequiredText(storageKey, MaxStorageKeyLength, DomainErrors.Document.StorageKeyEmpty, DomainErrors.Document.StorageKeyTooLong);
        if (keyResult.IsFailure)
            return Result.Failure<Document>(keyResult.Error);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.Document.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<Document>(descriptionResult.Error);

        var uploaderResult = Guard.NotEmpty(uploadedByUserId, DomainErrors.Document.UploadedByUserIdEmpty);
        if (uploaderResult.IsFailure)
            return Result.Failure<Document>(uploaderResult.Error);

        return new Document(Guid.CreateVersion7())
        {
            Owner = owner,
            FileName = nameResult.Value,
            ContentType = typeResult.Value,
            SizeBytes = sizeBytes,
            StorageKey = keyResult.Value,
            Description = descriptionResult.Value,
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = utcNow
        };
    }

    /// <summary>Changes the description of the document.</summary>
    /// <returns>A failure if deletion was requested or the description is too long.</returns>
    public Result UpdateDescription(string? description)
    {
        if (IsDeleted)
            return Result.Failure(DomainErrors.Document.AlreadyDeleted);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.Document.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure(descriptionResult.Error);

        Description = descriptionResult.Value;

        return Result.Success();
    }

    /// <summary>
    /// Requests deletion of the document and raises <see cref="DocumentDeletedDomainEvent"/>, so that the file is removed
    /// from storage after the record is deleted.
    /// </summary>
    /// <returns>A failure if deletion was already requested.</returns>
    public Result Delete(DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(DomainErrors.Document.AlreadyDeleted);

        IsDeleted = true;
        RaiseDomainEvent(new DocumentDeletedDomainEvent(Id, Owner.Kind, Owner.Id, StorageKey, utcNow));

        return Result.Success();
    }

    private static Result<string> ValidateFileName(string? fileName)
    {
        var nameResult = Guard.RequiredText(fileName, MaxFileNameLength, DomainErrors.Document.FileNameEmpty, DomainErrors.Document.FileNameTooLong);
        if (nameResult.IsFailure)
            return nameResult;

        var name = nameResult.Value;
        if (name.Contains('/', StringComparison.Ordinal) || name.Contains('\\', StringComparison.Ordinal)
            || name.Any(char.IsControl) || name is "." or "..")
            return Result.Failure<string>(DomainErrors.Document.FileNameInvalid);

        return name;
    }

    private static Result<string> ValidateContentType(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_contentTypeByExtension.TryGetValue(extension, out var expectedType))
            return Result.Failure<string>(DomainErrors.Document.FormatNotAllowed);

        var declaredType = contentType?.Trim().ToLowerInvariant();
        if (declaredType != expectedType)
            return Result.Failure<string>(DomainErrors.Document.ContentTypeMismatch);

        return expectedType;
    }
}
