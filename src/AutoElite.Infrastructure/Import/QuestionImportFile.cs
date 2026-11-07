using System.Text;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Infrastructure.Import;

/// <summary>
/// An imported file that has been read: its rows, and the archive whose images they name if the file came in one.
/// A file of questions uploaded alone has no images, so a row that names one is reported as missing it.
/// </summary>
/// <param name="rows">The rows.</param>
/// <param name="archive">The archive of the import, which the file then owns, or <see langword="null"/>.</param>
internal sealed class QuestionImportFile(IReadOnlyList<QuestionImportRow> rows, ImportArchive? archive) : IQuestionImportFile
{
    /// <inheritdoc />
    public IReadOnlyList<QuestionImportRow> Rows => rows;

    /// <inheritdoc />
    public long? GetImageSize(string imageName) => archive?.Find(imageName)?.Length;

    /// <inheritdoc />
    public Task<Stream> OpenImageAsync(string imageName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = archive?.Find(imageName)
            ?? throw new ArgumentException($"The file has no image \"{imageName}\".", nameof(imageName));

        return Task.FromResult(ImportArchive.OpenRead(entry));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        archive?.Dispose();

        return ValueTask.CompletedTask;
    }
}

/// <summary>The questions of one file, as its format reader returns them.</summary>
/// <param name="Rows">The rows, numbered as the file numbers them.</param>
/// <param name="RepeatsQuestions">
/// Whether the format repeats a question in several places, as the tickets of <c>pdd_russia</c> do, so that a row
/// that is the same as an earlier one in every part is dropped instead of being reported as a repeated code.
/// </param>
internal sealed record ParsedQuestionFile(IReadOnlyList<QuestionImportRow> Rows, bool RepeatsQuestions);

/// <summary>Puts the rows of the files of an import together.</summary>
internal static class QuestionRows
{
    // Separates the parts of a row in its key; it cannot be typed and does not occur in text.
    private const char Separator = '\u001F';

    /// <summary>
    /// Puts the rows of the files together in order, dropping the repeats of the formats that repeat questions. The rows
    /// of a single file keep its numbers; when there are several files, the rows are numbered from 1 through all of
    /// them, so that every row has a number of its own.
    /// </summary>
    /// <param name="files">The files, in the order they are read.</param>
    public static IReadOnlyList<QuestionImportRow> Combine(IReadOnlyList<ParsedQuestionFile> files)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<QuestionImportRow>();
        foreach (var file in files)
        {
            foreach (var row in file.Rows)
            {
                if (!file.RepeatsQuestions || seen.Add(KeyOf(row)))
                    rows.Add(row);
            }
        }

        return files.Count == 1
            ? rows
            : [.. rows.Select((row, index) => row with { RowNumber = index + 1 })];
    }

    private static string KeyOf(QuestionImportRow row)
    {
        var key = new StringBuilder();
        foreach (var part in new[] { row.ExternalCode, row.Text, row.Explanation, row.ImageName, string.Join(',', row.Categories) })
            key.Append(part).Append(Separator);

        foreach (var option in row.Options)
            key.Append(option.IsCorrect ? '+' : '-').Append(option.Text).Append(Separator);

        return key.ToString();
    }
}
