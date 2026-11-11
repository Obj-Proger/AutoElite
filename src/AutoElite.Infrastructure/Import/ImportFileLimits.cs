namespace AutoElite.Infrastructure.Import;

/// <summary>The size of an imported file that the host lets a request upload before the import reads it.</summary>
public static class ImportFileLimits
{
    /// <summary>The largest file the import accepts: an archive of questions with their images.</summary>
    public const long MaxFileBytes = ImportLimits.MaxArchiveBytes;
}
