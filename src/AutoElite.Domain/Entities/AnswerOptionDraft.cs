namespace AutoElite.Domain.Entities;

/// <summary>The input for one answer option of a <see cref="Question"/>, validated when the question is created or updated.</summary>
/// <param name="Text">The text of the option.</param>
/// <param name="IsCorrect">Whether this option is the correct answer.</param>
public sealed record AnswerOptionDraft(string? Text, bool IsCorrect);
