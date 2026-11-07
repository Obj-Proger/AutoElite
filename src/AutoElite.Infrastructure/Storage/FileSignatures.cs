using System.Text;

namespace AutoElite.Infrastructure.Storage;

/// <summary>
/// Checks that a file's content is of its declared type, by the signature at its start, since both the type and the
/// name come from the client. Office documents are ZIP packages; plain text must be UTF-8 without NUL characters.
/// </summary>
internal static class FileSignatures
{
    /// <summary>The bytes from the start of the file that the signatures need.</summary>
    public const int HeaderLength = 12;

    private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Tells whether the start of a file fits its content type.</summary>
    /// <param name="contentType">The content type, in lower case.</param>
    /// <param name="header">The first bytes of the file, up to <see cref="HeaderLength"/>.</param>
    public static bool Matches(string contentType, ReadOnlySpan<byte> header) =>
        contentType switch
        {
            "application/pdf" => header.StartsWith("%PDF-"u8),
            "image/jpeg" => header.StartsWith(Jpeg),
            "image/png" => header.StartsWith(Png),
            "image/webp" => header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                or "application/vnd.openxmlformats-officedocument.presentationml.presentation" =>
                header.StartsWith("PK\u0003\u0004"u8),
            "text/plain" => true,
            _ => false
        };

    /// <summary>Tells whether a content type is text, whose whole content is checked as it is read.</summary>
    /// <param name="contentType">The content type, in lower case.</param>
    public static bool IsText(string contentType) => contentType == "text/plain";

    /// <summary>Creates the check of a text's content, fed with the content piece by piece.</summary>
    public static TextCheck CreateTextCheck() => new();

    /// <summary>Checks a text piece by piece: UTF-8, a character may be split between pieces, and no NUL characters.</summary>
    public sealed class TextCheck
    {
        private readonly Decoder _decoder = new UTF8Encoding(false, throwOnInvalidBytes: true).GetDecoder();
        private readonly char[] _chars = new char[8192];

        /// <summary>Gets a value indicating whether the text read so far is valid.</summary>
        public bool IsValid { get; private set; } = true;

        /// <summary>Checks the next piece of the text.</summary>
        /// <param name="bytes">The piece.</param>
        /// <param name="isLast">Whether it is the last piece, after which no character may be left incomplete.</param>
        public void Feed(ReadOnlySpan<byte> bytes, bool isLast)
        {
            if (!IsValid)
                return;

            if (bytes.Contains((byte)0))
            {
                IsValid = false;
                return;
            }

            try
            {
                var completed = false;
                while (!completed)
                {
                    _decoder.Convert(bytes, _chars, isLast, out var bytesUsed, out _, out completed);
                    bytes = bytes[bytesUsed..];
                }
            }
            catch (DecoderFallbackException)
            {
                IsValid = false;
            }
        }
    }
}
