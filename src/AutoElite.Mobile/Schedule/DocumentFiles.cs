using System.Globalization;
using AutoElite.Client.Api;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The files of documents the user opens, from a chat or their documents: each is downloaded once into the cache of
/// the application and opened in another application. They are deleted when the user signs out, so that the next
/// user of the device does not find them.
/// </summary>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class DocumentFiles(Func<AutoEliteApiClient> createApi)
{
    /// <summary>Gets the folder of the files the user opened, in the cache of the application.</summary>
    public static string Folder => Path.Combine(FileSystem.CacheDirectory, "documents");

    /// <summary>Deletes the files the user opened.</summary>
    public static void Forget()
    {
        try
        {
            if (Directory.Exists(Folder))
                Directory.Delete(Folder, recursive: true);
        }
        catch (IOException)
        {
            // A file still open elsewhere stays until the next sign-out; the cache is private to the application.
        }
    }

    /// <summary>Downloads a file, once, and opens it in another application.</summary>
    /// <param name="documentId">The document of the file.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the file is handed to another application.</returns>
    public async Task OpenAsync(Guid documentId, string fileName, CancellationToken cancellationToken)
    {
        var path = await DownloadAsync(documentId, fileName, cancellationToken);
        await Launcher.Default.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path)));
    }

    private async Task<string> DownloadAsync(Guid documentId, string fileName, CancellationToken cancellationToken)
    {
        // The name comes from another user: only its last part, without characters the file system refuses.
        var invalid = Path.GetInvalidFileNameChars();
        var name = string.Concat(Path.GetFileName(fileName).Select(c => invalid.Contains(c) ? '_' : c));
        var folder = Path.Combine(Folder, documentId.ToString("N", CultureInfo.InvariantCulture));
        var path = Path.Combine(folder, string.IsNullOrWhiteSpace(name) ? "file" : name);
        if (File.Exists(path))
            return path;

        Directory.CreateDirectory(folder);
        await using var content = await createApi().Api.V1.Documents[documentId].GetAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API answered without the file.");
        var partial = path + ".part";
        await using (var target = File.Create(partial))
            await content.CopyToAsync(target, cancellationToken);

        File.Move(partial, path, overwrite: true);

        return path;
    }
}
