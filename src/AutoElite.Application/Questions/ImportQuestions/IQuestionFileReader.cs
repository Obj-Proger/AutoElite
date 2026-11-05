namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>
/// Reads the questions of an imported file (FR-7.1, O-13). The infrastructure decides which formats it reads; the
/// application checks every question the same way whatever the format.
/// </summary>
public interface IQuestionFileReader
{
    /// <summary>Reads the rows of the file; it does not check the questions themselves.</summary>
    /// <param name="content">The content of the file, read once.</param>
    /// <param name="fileName">The name of the file, whose extension tells the format.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>
    /// The rows, or <see cref="ApplicationErrors.Question.ImportFormatNotSupported"/> or
    /// <see cref="ApplicationErrors.Question.ImportUnreadable"/>.
    /// </returns>
    Task<Result<IReadOnlyList<QuestionImportRow>>> ReadAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default);
}
