namespace AutoElite.Infrastructure.Storage;

/// <summary>Where the files of documents are kept (D-069).</summary>
public enum FileStorageProvider
{
    /// <summary>A folder on the server's disk, the default for a school's own server.</summary>
    Local
}

/// <summary>
/// Where and how the files of documents are kept, from the <see cref="SectionName"/> configuration section (D-069).
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "FileStorage";

    /// <summary>Gets or sets where the files are kept.</summary>
    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    /// <summary>
    /// Gets or sets the folder of the <see cref="FileStorageProvider.Local"/> provider: an absolute path outside the
    /// application's folder, which the application's account may write and nobody may execute from, and which is
    /// backed up together with the database.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how old a stored file without a document must be before the daily clean-up removes it; younger
    /// ones may belong to a document whose save has not committed yet.
    /// </summary>
    public TimeSpan OrphanAge { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (Provider == FileStorageProvider.Local)
        {
            if (string.IsNullOrWhiteSpace(RootPath) || !Path.IsPathFullyQualified(RootPath))
            {
                yield return $"{SectionName}:RootPath must be the absolute path of the folder that keeps the files.";
            }
            else if (IsWithin(Path.GetFullPath(RootPath), Path.GetFullPath(AppContext.BaseDirectory)))
            {
                yield return $"{SectionName}:RootPath must be outside the application's folder, so that files cannot "
                    + "be served or executed as part of it.";
            }
        }

        if (OrphanAge < TimeSpan.FromHours(1))
            yield return $"{SectionName}:OrphanAge must be at least an hour.";
    }

    private static bool IsWithin(string path, string folder)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var relative = Path.GetRelativePath(folder, path);

        return string.Equals(relative, ".", comparison)
            || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }
}
