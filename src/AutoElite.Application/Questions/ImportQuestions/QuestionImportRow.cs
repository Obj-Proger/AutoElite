namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>A question as read from a row of an imported file; the import checks it like a question typed in.</summary>
/// <param name="RowNumber">The number of the row in the file, for error messages.</param>
/// <param name="Text">The question text.</param>
/// <param name="Explanation">The explanation shown after the test, if any.</param>
/// <param name="Categories">The licence categories as written in the file, for example <c>B</c> or <c>C1E</c>.</param>
/// <param name="ExternalCode">The code of the question in its source, if any; a known code updates that question.</param>
/// <param name="Options">The answer options in order, exactly one correct.</param>
/// <param name="ImageName">
/// The name of the illustration in the file, if any, for <see cref="IQuestionImportFile.OpenImageAsync"/>; it may
/// contain folders, and the last part becomes the name of the document.
/// </param>
public sealed record QuestionImportRow(
    int RowNumber,
    string? Text,
    string? Explanation,
    IReadOnlyList<string> Categories,
    string? ExternalCode,
    IReadOnlyList<AnswerOptionInput> Options,
    string? ImageName = null);

/// <summary>What an import did.</summary>
/// <param name="Created">The questions added.</param>
/// <param name="Updated">The questions updated because the bank already had their external codes.</param>
/// <param name="Images">The illustrations stored, each as a new document of its question.</param>
public sealed record QuestionImportResult(int Created, int Updated, int Images);
