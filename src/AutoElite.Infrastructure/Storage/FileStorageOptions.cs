namespace AutoElite.Infrastructure.Storage;

/// <summary>Where the files of documents are kept (D-069).</summary>
public enum FileStorageProvider
{
    /// <summary>A folder on the server's disk, the default for a school's own server.</summary>
    Local,

    /// <summary>
    /// An S3-compatible object storage, such as Selectel, Yandex Object Storage, or a school's own RustFS or SeaweedFS.
    /// </summary>
    S3
}

/// <summary>The bucket of the <see cref="FileStorageProvider.S3"/> provider.</summary>
public sealed class S3StorageOptions
{
    /// <summary>Gets or sets the address of the storage's S3 API, such as <c>https://s3.storage.selcloud.ru</c>.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the region the requests are signed for; the storage's documentation names it.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Gets or sets the bucket, which must not be public: files are only downloaded through the API.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Gets or sets the access key of the account.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret key of the account; a secret, set like the database password.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the bucket is addressed in the path rather than the host name, as most compatible
    /// storages expect.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Gets or sets the absolute path of the local folder that files are received into and checked in before they are
    /// uploaded; the system's temporary folder by default.
    /// </summary>
    public string? StagingPath { get; set; }

    /// <summary>Gets the folder that files are received into.</summary>
    internal string StagingFolder => string.IsNullOrWhiteSpace(StagingPath)
        ? Path.Combine(Path.GetTempPath(), "autoelite-incoming")
        : StagingPath;
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

    /// <summary>Gets the bucket of the <see cref="FileStorageProvider.S3"/> provider.</summary>
    public S3StorageOptions S3 { get; } = new();

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

        if (Provider == FileStorageProvider.S3)
        {
            if (!Uri.TryCreate(S3.ServiceUrl, UriKind.Absolute, out var serviceUrl)
                || serviceUrl.Scheme is not ("https" or "http"))
            {
                yield return $"{SectionName}:S3:ServiceUrl must be the address of the storage's S3 API.";
            }

            if (string.IsNullOrWhiteSpace(S3.BucketName))
                yield return $"{SectionName}:S3:BucketName must name the bucket.";

            if (string.IsNullOrWhiteSpace(S3.AccessKey) || string.IsNullOrWhiteSpace(S3.SecretKey))
            {
                yield return $"{SectionName}:S3:AccessKey and {SectionName}:S3:SecretKey must be set; set the secret key "
                    + $"with dotnet user-secrets locally or the {SectionName}__S3__SecretKey environment variable.";
            }

            if (!string.IsNullOrWhiteSpace(S3.StagingPath) && !Path.IsPathFullyQualified(S3.StagingPath))
                yield return $"{SectionName}:S3:StagingPath must be an absolute path.";
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
