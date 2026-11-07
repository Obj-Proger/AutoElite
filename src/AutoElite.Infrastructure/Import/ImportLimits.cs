using System.Globalization;

namespace AutoElite.Infrastructure.Import;

/// <summary>
/// The limits of an imported file, which comes from a user and may be damaged or made to exhaust the server: they bound
/// what is read into memory, what an archive may unpack to and how deep a JSON file nests.
/// </summary>
internal static class ImportLimits
{
    /// <summary>The largest file of questions, uploaded alone or packed in an archive; it is read into memory.</summary>
    public const long MaxQuestionFileBytes = 32L * 1024 * 1024;

    /// <summary>The largest archive, which is kept in a temporary file while the import runs.</summary>
    public const long MaxArchiveBytes = 512L * 1024 * 1024;

    /// <summary>The most an archive may unpack to, by the sizes its entries declare.</summary>
    public const long MaxArchiveContentBytes = 1024L * 1024 * 1024;

    /// <summary>The most an Excel workbook may unpack to; a workbook of 2000 questions takes a few megabytes.</summary>
    public const long MaxWorkbookContentBytes = 128L * 1024 * 1024;

    /// <summary>The most entries an archive or a workbook may have.</summary>
    public const int MaxArchiveEntries = 10_000;

    /// <summary>The most files of questions an archive may have.</summary>
    public const int MaxQuestionFiles = 200;

    /// <summary>The highest ratio of the unpacked to the packed size of a large entry; text packs about ten times.</summary>
    public const int MaxCompressionRatio = 100;

    /// <summary>The unpacked size from which the ratio of an entry is checked, since small entries pack unevenly.</summary>
    public const long CompressionCheckFromBytes = 1024 * 1024;

    /// <summary>The deepest nesting of a JSON file; the formats need four levels.</summary>
    public const int MaxJsonDepth = 16;

    /// <summary>The most columns of a worksheet that are read.</summary>
    public const int MaxColumns = 100;

    /// <summary>Describes a size in whole megabytes, for the messages of the limits.</summary>
    /// <param name="bytes">The size in bytes.</param>
    public static string Megabytes(long bytes) =>
        string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024 * 1024)} MB");
}
