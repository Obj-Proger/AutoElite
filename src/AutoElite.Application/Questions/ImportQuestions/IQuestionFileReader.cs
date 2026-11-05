namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>
/// Reads the questions of an imported file (FR-7.1, D-070). The infrastructure decides which formats it reads; the
/// application checks every question the same way whatever the format.
/// </summary>
public interface IQuestionFileReader
{
    /// <summary>Reads the rows of the file and finds their images; it does not check the questions themselves.</summary>
    /// <param name="content">The content of the file, read once.</param>
    /// <param name="fileName">The name of the file; its extension and content tell the format.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>
    /// The file read, which the caller disposes, or <see cref="ApplicationErrors.Question.ImportFormatNotSupported"/> or
    /// <see cref="ApplicationErrors.Question.ImportUnreadable"/>.
    /// </returns>
    Task<Result<IQuestionImportFile>> ReadAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// An imported file that has been read: its rows and the images they name, for example the pictures packed in a ZIP
/// archive next to the questions or embedded in the file. Disposing it releases what the reader kept, such as
/// temporary files.
/// </summary>
public interface IQuestionImportFile : IAsyncDisposable
{
    /// <summary>Gets the rows of the file, in order.</summary>
    IReadOnlyList<QuestionImportRow> Rows { get; }

    /// <summary>Gets the size of an image of the file.</summary>
    /// <param name="imageName">The name of the image, as a row gives it.</param>
    /// <returns>The size in bytes, or <see langword="null"/> if the file has no such image.</returns>
    long? GetImageSize(string imageName);

    /// <summary>Opens an image of the file for reading; the caller disposes the stream.</summary>
    /// <param name="imageName">The name of an image the file has.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<Stream> OpenImageAsync(string imageName, CancellationToken cancellationToken = default);
}
