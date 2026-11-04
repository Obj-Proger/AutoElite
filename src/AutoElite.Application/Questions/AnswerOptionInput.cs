namespace AutoElite.Application.Questions;

/// <summary>An answer option of a question, in the order shown to the student.</summary>
/// <param name="Text">The text of the option.</param>
/// <param name="IsCorrect">Whether this is the correct answer; exactly one option is.</param>
public sealed record AnswerOptionInput(string? Text, bool IsCorrect);
