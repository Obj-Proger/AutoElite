namespace AutoElite.Application.Documents;

/// <summary>Tells which documents are images that can be shown as a photo or an illustration.</summary>
internal static class DocumentImages
{
    // Lower-case extension -> the content type of the image, as the domain expects it.
    private static readonly Dictionary<string, string> _contentTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private static readonly HashSet<string> _imageTypes = [.. _contentTypeByExtension.Values];

    /// <summary>Determines whether the document is a JPEG, PNG or WebP image.</summary>
    /// <param name="document">The document.</param>
    public static bool IsImage(Document document) => _imageTypes.Contains(document.ContentType);

    /// <summary>Gets the content type of an image file by its extension.</summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The content type, or <see langword="null"/> if the extension is not that of a JPEG, PNG or WebP image.</returns>
    public static string? ContentTypeOf(string fileName) =>
        _contentTypeByExtension.GetValueOrDefault(Path.GetExtension(fileName));
}
