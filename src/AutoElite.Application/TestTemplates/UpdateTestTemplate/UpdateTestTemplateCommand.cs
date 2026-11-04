namespace AutoElite.Application.TestTemplates.UpdateTestTemplate;

/// <summary>Replaces the content of an active template; tests already started keep the rules they started with.</summary>
/// <param name="TemplateId">The identifier of the template.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category the questions are drawn for.</param>
/// <param name="QuestionCount">The number of questions, from 1 to 100; the sum of the blocks.</param>
/// <param name="TimeLimitMinutes">The time limit, from 1 minute to 3 hours.</param>
/// <param name="MaxMistakes">The mistakes allowed in the whole test, fewer than the questions.</param>
/// <param name="MaxMistakesPerBlock">The mistakes allowed in one block, if limited.</param>
/// <param name="ExtraQuestionsPerMistake">The questions added for each mistake, from 0 to 20.</param>
/// <param name="ExtraMinutesPerMistake">The minutes added for each mistake, from 0 to 30; only with added questions.</param>
/// <param name="Blocks">From 1 to 10 blocks; a block of all topics must be the only one.</param>
/// <param name="Source">Only official or only school questions; empty for both.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record UpdateTestTemplateCommand(
    Guid TemplateId,
    string? Name,
    LicenseCategory Category,
    int QuestionCount,
    int TimeLimitMinutes,
    int MaxMistakes,
    int? MaxMistakesPerBlock,
    int ExtraQuestionsPerMistake,
    int ExtraMinutesPerMistake,
    IReadOnlyList<TestBlockInput>? Blocks,
    QuestionSource? Source = null) : ICommand, ITestTemplateDefinition;
