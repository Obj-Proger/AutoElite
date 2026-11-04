namespace AutoElite.Application.TestTemplates;

/// <summary>A row of the template list.</summary>
/// <param name="Id">The identifier of the template.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category.</param>
/// <param name="QuestionCount">The number of questions.</param>
/// <param name="TimeLimitMinutes">The time limit in minutes.</param>
/// <param name="MaxMistakes">The mistakes allowed in the whole test.</param>
/// <param name="Source">The source the questions are drawn from, or <see langword="null"/> for both.</param>
/// <param name="IsActive">Whether the template is offered for new tests.</param>
public sealed record TestTemplateItem(
    Guid Id,
    string Name,
    LicenseCategory Category,
    int QuestionCount,
    int TimeLimitMinutes,
    int MaxMistakes,
    QuestionSource? Source,
    bool IsActive);

/// <summary>A topic of a test block.</summary>
/// <param name="Id">The identifier of the topic.</param>
/// <param name="Title">The title of the topic.</param>
public sealed record TestBlockTopicItem(Guid Id, string Title);

/// <summary>A block of a template.</summary>
/// <param name="QuestionCount">The number of questions drawn for the block.</param>
/// <param name="Topics">The topics drawn from, empty for all topics.</param>
public sealed record TestBlockItem(int QuestionCount, IReadOnlyList<TestBlockTopicItem> Topics);

/// <summary>The card of a template with its rules and blocks.</summary>
/// <param name="Id">The identifier of the template.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category.</param>
/// <param name="QuestionCount">The number of questions.</param>
/// <param name="TimeLimitMinutes">The time limit in minutes.</param>
/// <param name="MaxMistakes">The mistakes allowed in the whole test.</param>
/// <param name="MaxMistakesPerBlock">The mistakes allowed in one block, if limited.</param>
/// <param name="ExtraQuestionsPerMistake">The questions added for each mistake.</param>
/// <param name="ExtraMinutesPerMistake">The minutes added for each mistake.</param>
/// <param name="Source">The source the questions are drawn from, or <see langword="null"/> for both.</param>
/// <param name="IsActive">Whether the template is offered for new tests.</param>
/// <param name="Blocks">The blocks in order.</param>
public sealed record TestTemplateDetails(
    Guid Id,
    string Name,
    LicenseCategory Category,
    int QuestionCount,
    int TimeLimitMinutes,
    int MaxMistakes,
    int? MaxMistakesPerBlock,
    int ExtraQuestionsPerMistake,
    int ExtraMinutesPerMistake,
    QuestionSource? Source,
    bool IsActive,
    IReadOnlyList<TestBlockItem> Blocks);
