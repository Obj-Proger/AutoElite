namespace AutoElite.Application.Questions.CreateQuestion;

/// <summary>
/// Adds a question to the bank (FR-7.1). Administrators add official and school questions; theory instructors add
/// school questions. An image is attached separately, once documents are stored.
/// </summary>
/// <param name="Text">The question text.</param>
/// <param name="Explanation">The explanation shown after the test, if any.</param>
/// <param name="TopicId">A course topic the question belongs to.</param>
/// <param name="Categories">The licence categories the question applies to, at least one.</param>
/// <param name="Source">Whether the question is official or written by the school.</param>
/// <param name="ExternalCode">The code of the question in the official set, unique within the bank, if any.</param>
/// <param name="Options">From 2 to 6 answer options with exactly one correct answer.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record CreateQuestionCommand(
    string? Text,
    string? Explanation,
    Guid TopicId,
    IReadOnlyList<LicenseCategory>? Categories,
    QuestionSource Source,
    string? ExternalCode,
    IReadOnlyList<AnswerOptionInput>? Options) : ICommand<Guid>;
