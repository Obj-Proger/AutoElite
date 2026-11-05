namespace AutoElite.Application.Documents;

/// <summary>Tells which documents are images that can be shown as a photo or an illustration.</summary>
internal static class DocumentImages
{
    private static readonly HashSet<string> _imageTypes = ["image/jpeg", "image/png", "image/webp"];

    /// <summary>Determines whether the document is a JPEG, PNG or WebP image.</summary>
    /// <param name="document">The document.</param>
    public static bool IsImage(Document document) => _imageTypes.Contains(document.ContentType);
}
