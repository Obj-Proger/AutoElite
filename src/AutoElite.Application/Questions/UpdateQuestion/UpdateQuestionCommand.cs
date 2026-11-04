namespace AutoElite.Application.Questions.UpdateQuestion;

/// <summary>
/// Replaces the content of an active question; its source and image stay. Tests already taken keep the questions as
/// they were asked.
/// </summary>
/// <param name="QuestionId">The identifier of the question.</param>
/// <param name="Text">The question text.</param>
/// <param name="Explanation">The explanation shown after the test; empty removes it.</param>
/// <param name="TopicId">A course topic the question belongs to.</param>
/// <param name="Categories">The licence categories the question applies to, at least one.</param>
/// <param name="ExternalCode">The code of the question in the official set; empty removes it.</param>
/// <param name="Options">From 2 to 6 answer options with exactly one correct answer.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record UpdateQuestionCommand(
    Guid QuestionId,
    string? Text,
    string? Explanation,
    Guid TopicId,
    IReadOnlyList<LicenseCategory>? Categories,
    string? ExternalCode,
    IReadOnlyList<AnswerOptionInput>? Options) : ICommand;
