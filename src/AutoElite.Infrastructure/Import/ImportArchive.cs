using System.Globalization;
using System.IO.Compression;

namespace AutoElite.Infrastructure.Import;

/// <summary>
/// The ZIP archive of an import: one or more files of questions and the images they name, in any folders. The archive
/// is not unpacked to disk; an entry is read when it is needed and never past the size it declares, so that a damaged
/// or crafted archive (a zip bomb) cannot fill the memory or the disk, and no entry name can lead outside a folder.
/// An archive with an unsafe or a repeated name is refused all the same. Disposing it closes the archive and the
/// stream it reads.
/// </summary>
internal sealed class ImportArchive : IDisposable
{
    // The folder of resource forks that the archiver of macOS adds; it holds no files of the import.
    private const string MacOsFolder = "__MACOSX/";

    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;
    private readonly Dictionary<string, ZipArchiveEntry?> _found = new(StringComparer.Ordinal);

    private ImportArchive(ZipArchive archive, Dictionary<string, ZipArchiveEntry> entries)
    {
        _archive = archive;
        _entries = entries;
        QuestionFiles =
        [
            .. entries
                .Where(entry => QuestionFileReader.KindOf(entry.Key) is { } kind && kind != QuestionFileKind.Zip)
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
        ];
    }

    /// <summary>Gets the files of questions with their paths, in the ordinal order of the paths.</summary>
    public IReadOnlyList<KeyValuePair<string, ZipArchiveEntry>> QuestionFiles { get; }

    /// <summary>Opens an archive and checks its limits and names.</summary>
    /// <param name="content">A seekable stream of the archive, which the archive then owns.</param>
    /// <exception cref="InvalidDataException">The content is not a ZIP archive, or the archive breaks a limit.</exception>
    public static ImportArchive Open(Stream content)
    {
        var archive = ZipPackages.Open(content, leaveOpen: false, "The file is not a valid ZIP archive.");
        try
        {
            ArchiveLimits.Check(archive, ImportLimits.MaxArchiveContentBytes);
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                // A folder has an empty name.
                if (entry.Name.Length == 0)
                    continue;

                var path = ImportPaths.Normalize(entry.FullName)
                    ?? throw new InvalidDataException($"The archive has a file with an unsafe name \"{entry.FullName}\".");
                if (path.StartsWith(MacOsFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!entries.TryAdd(path, entry))
                    throw new InvalidDataException($"The archive has \"{path}\" more than once.");
            }

            return new ImportArchive(archive, entries);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Finds a file by the name a question gives: its path in the archive, or the end of a path, so that a name relative
    /// to the top folder of the source still finds the file when the archive puts that folder in another one.
    /// </summary>
    /// <param name="name">The name, with forward or back slashes; letter case is ignored.</param>
    /// <returns>The entry, or <see langword="null"/> if no file, or more than one, has the name.</returns>
    public ZipArchiveEntry? Find(string name)
    {
        if (_found.TryGetValue(name, out var cached))
            return cached;

        var entry = Lookup(name);
        _found[name] = entry;

        return entry;
    }

    /// <summary>Opens an entry for reading, never past its declared size; the caller disposes the stream.</summary>
    /// <param name="entry">The entry.</param>
    public static Stream OpenRead(ZipArchiveEntry entry) => new BoundedReadStream(entry.Open(), entry.Length);

    /// <summary>Reads a file of questions into memory.</summary>
    /// <param name="entry">The entry of the file.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <exception cref="InvalidDataException">The file is too large, or the archive is damaged.</exception>
    public static async Task<byte[]> ReadAllAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        if (entry.Length > ImportLimits.MaxQuestionFileBytes)
            throw new InvalidDataException($"The file is larger than {ImportLimits.Megabytes(ImportLimits.MaxQuestionFileBytes)}.");

        await using var content = OpenRead(entry);
        using var copy = new MemoryStream((int)entry.Length);
        await content.CopyToAsync(copy, cancellationToken);

        return copy.ToArray();
    }

    /// <inheritdoc />
    public void Dispose() => _archive.Dispose();

    private ZipArchiveEntry? Lookup(string name)
    {
        if (ImportPaths.Normalize(name) is not { Length: > 0 } path)
            return null;

        if (_entries.TryGetValue(path, out var exact))
            return exact;

        var ending = "/" + path;
        ZipArchiveEntry? match = null;
        foreach (var (entryPath, entry) in _entries)
        {
            if (!entryPath.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
                continue;

            if (match is not null)
                return null;

            match = entry;
        }

        return match;
    }
}

/// <summary>The limits of a ZIP archive, shared by the archives of an import and the Excel workbooks.</summary>
internal static class ArchiveLimits
{
    /// <summary>Checks the number of entries, the size they unpack to and how tightly each is packed.</summary>
    /// <param name="archive">The archive.</param>
    /// <param name="maxContentBytes">The most the entries may unpack to together.</param>
    /// <exception cref="InvalidDataException">The archive breaks a limit.</exception>
    public static void Check(ZipArchive archive, long maxContentBytes)
    {
        var entries = archive.Entries;
        if (entries.Count > ImportLimits.MaxArchiveEntries)
        {
            throw new InvalidDataException(string.Create(
                CultureInfo.InvariantCulture, $"The archive has more than {ImportLimits.MaxArchiveEntries} files."));
        }

        long total = 0;
        foreach (var entry in entries)
        {
            if (entry.Length > maxContentBytes - total)
                throw new InvalidDataException($"The archive unpacks to more than {ImportLimits.Megabytes(maxContentBytes)}.");

            total += entry.Length;
            if (entry.Length >= ImportLimits.CompressionCheckFromBytes
                && entry.Length / Math.Max(entry.CompressedLength, 1) > ImportLimits.MaxCompressionRatio)
            {
                throw new InvalidDataException($"\"{entry.FullName}\" is packed too tightly to be a file of an import.");
            }
        }
    }
}

/// <summary>Opens the ZIP packages of an import, archives and Excel workbooks.</summary>
internal static class ZipPackages
{
    /// <summary>Opens a package for reading, reporting content that is not one in words of the import.</summary>
    /// <param name="content">A seekable stream of the package.</param>
    /// <param name="leaveOpen">Whether the stream stays open when the package is disposed.</param>
    /// <param name="notAPackage">The reason given when the content is not a ZIP package.</param>
    /// <exception cref="InvalidDataException">The content is not a ZIP package.</exception>
    public static ZipArchive Open(Stream content, bool leaveOpen, string notAPackage)
    {
        try
        {
            return new ZipArchive(content, ZipArchiveMode.Read, leaveOpen);
        }
        catch (InvalidDataException exception)
        {
            // The message of the runtime describes the format, not the file, and may be in the language of the server.
            throw new InvalidDataException(notAPackage, exception);
        }
    }
}

/// <summary>The paths of files in an archive and in the questions that name them.</summary>
internal static class ImportPaths
{
    /// <summary>
    /// Brings a path to one form: forward slashes, without empty and <c>.</c> parts, so that <c>./images/1.jpg</c> and
    /// <c>images\1.jpg</c> are the same path.
    /// </summary>
    /// <param name="name">The path.</param>
    /// <returns>The path, or <see langword="null"/> if it is absolute or goes up with <c>..</c>.</returns>
    public static string? Normalize(string name)
    {
        var path = name.Replace('\\', '/');
        if (path.StartsWith('/') || path.Contains(':', StringComparison.Ordinal))
            return null;

        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part.Length == 0 || part == ".")
                continue;

            if (part == "..")
                return null;

            parts.Add(part);
        }

        return string.Join('/', parts);
    }
}
