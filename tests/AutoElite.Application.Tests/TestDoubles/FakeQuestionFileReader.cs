using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns the rows or the error the test gives; the parsing of files is the infrastructure's job.</summary>
internal sealed class FakeQuestionFileReader : IQuestionFileReader
{
    public IReadOnlyList<QuestionImportRow> Rows { get; set; } = [];

    public Error? Failure { get; set; }

    public Task<Result<IReadOnlyList<QuestionImportRow>>> ReadAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is { } error ? Result.Failure<IReadOnlyList<QuestionImportRow>>(error) : Result.Success(Rows));

    /// <summary>Builds a valid row with two options, the first one correct.</summary>
    public static QuestionImportRow Row(int number, string? externalCode = null, string text = "What does the sign mean?") =>
        new(number, text, null, ["B"], externalCode, [new AnswerOptionInput("Stop", true), new AnswerOptionInput("Go", false)]);
}
