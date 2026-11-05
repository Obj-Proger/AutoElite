using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns the rows, images or error the test gives; the parsing of files is the infrastructure's job.</summary>
internal sealed class FakeQuestionFileReader : IQuestionFileReader
{
    private readonly Dictionary<string, byte[]> _images = [];

    public IReadOnlyList<QuestionImportRow> Rows { get; set; } = [];

    public Error? Failure { get; set; }

    /// <summary>Gets a value indicating whether the handler disposed the file it read.</summary>
    public bool FileDisposed { get; private set; }

    public Task<Result<IQuestionImportFile>> ReadAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (Failure is { } error)
            return Task.FromResult(Result.Failure<IQuestionImportFile>(error));

        // The handler owns the file it is given and disposes it.
        IQuestionImportFile file = new FakeQuestionImportFile(Rows, _images, () => FileDisposed = true);

        return Task.FromResult(Result.Success(file));
    }

    /// <summary>Puts an image into the file.</summary>
    public void AddImage(string imageName, byte[] bytes) => _images[imageName] = bytes;

    /// <summary>Builds a valid row with two options, the first one correct.</summary>
    public static QuestionImportRow Row(
        int number, string? externalCode = null, string text = "What does the sign mean?", string? imageName = null) =>
        new(number, text, null, ["B"], externalCode, [new AnswerOptionInput("Stop", true), new AnswerOptionInput("Go", false)],
            imageName);
}

/// <summary>A file read by <see cref="FakeQuestionFileReader"/>; it tells the reader when it is disposed.</summary>
internal sealed class FakeQuestionImportFile(
    IReadOnlyList<QuestionImportRow> rows, Dictionary<string, byte[]> images, Action onDisposed) : IQuestionImportFile
{
    public IReadOnlyList<QuestionImportRow> Rows => rows;

    public long? GetImageSize(string imageName) => images.TryGetValue(imageName, out var bytes) ? bytes.Length : null;

    public Task<Stream> OpenImageAsync(string imageName, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(images[imageName], writable: false));

    public ValueTask DisposeAsync()
    {
        onDisposed();

        return ValueTask.CompletedTask;
    }
}
