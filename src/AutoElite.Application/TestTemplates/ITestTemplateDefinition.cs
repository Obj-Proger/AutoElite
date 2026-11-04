namespace AutoElite.Application.TestTemplates;

/// <summary>
/// The content of a test template as the commands that write it carry it, with durations in whole minutes as on the
/// template form.
/// </summary>
internal interface ITestTemplateDefinition
{
    /// <summary>Gets the name of the template.</summary>
    string? Name { get; }

    /// <summary>Gets the licence category the questions are drawn for.</summary>
    LicenseCategory Category { get; }

    /// <summary>Gets the number of questions of the test, the sum of the blocks.</summary>
    int QuestionCount { get; }

    /// <summary>Gets the time limit in minutes.</summary>
    int TimeLimitMinutes { get; }

    /// <summary>Gets the number of mistakes allowed in the whole test.</summary>
    int MaxMistakes { get; }

    /// <summary>Gets the number of mistakes allowed in one block, if limited.</summary>
    int? MaxMistakesPerBlock { get; }

    /// <summary>Gets the number of questions added for each mistake.</summary>
    int ExtraQuestionsPerMistake { get; }

    /// <summary>Gets the minutes added for each mistake.</summary>
    int ExtraMinutesPerMistake { get; }

    /// <summary>Gets the blocks of the test.</summary>
    IReadOnlyList<TestBlockInput>? Blocks { get; }

    /// <summary>Gets the source the questions are drawn from, or <see langword="null"/> for both.</summary>
    QuestionSource? Source { get; }
}
