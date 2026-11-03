namespace AutoElite.Domain.ValueObjects;

/// <summary>
/// How a test is taken and scored: the number of questions, the time limit, how many mistakes are allowed, and the
/// add-on rule of the official exam (each mistake adds questions and time; a mistake in an added question fails).
/// </summary>
/// <remarks>
/// When <see cref="ExtraQuestionsPerMistake"/> is zero there are no added questions: the test is passed with at most
/// <see cref="MaxMistakes"/> mistakes. A test session copies the rules when it starts, so changing a template does not
/// affect attempts in progress.
/// </remarks>
public sealed class TestRules : ValueObject
{
    /// <summary>The most questions a test may have before any are added.</summary>
    public const int MaxQuestionCount = 100;

    /// <summary>The most questions one mistake may add.</summary>
    public const int MaxExtraQuestionsPerMistake = 20;

    /// <summary>The shortest time limit.</summary>
    public static readonly TimeSpan MinTimeLimit = TimeSpan.FromMinutes(1);

    /// <summary>The longest time limit.</summary>
    public static readonly TimeSpan MaxTimeLimit = TimeSpan.FromHours(3);

    /// <summary>The most time one mistake may add.</summary>
    public static readonly TimeSpan MaxExtraTimePerMistake = TimeSpan.FromMinutes(30);

    /// <summary>Gets the rules of the official traffic-police theory exam: 20 questions in 20 minutes, up to 2 mistakes, each adding 5 questions and 5 minutes.</summary>
    public static TestRules TrafficPoliceExam { get; } =
        new(20, TimeSpan.FromMinutes(20), 2, 5, TimeSpan.FromMinutes(5));

    /// <summary>Gets the number of questions before any are added.</summary>
    public int QuestionCount { get; }

    /// <summary>Gets the time limit before any time is added.</summary>
    public TimeSpan TimeLimit { get; }

    /// <summary>Gets the number of mistakes allowed in the main questions.</summary>
    public int MaxMistakes { get; }

    /// <summary>Gets the number of questions added for each mistake, or zero if none are added.</summary>
    public int ExtraQuestionsPerMistake { get; }

    /// <summary>Gets the time added for each mistake.</summary>
    public TimeSpan ExtraTimePerMistake { get; }

    /// <summary>Gets a value indicating whether mistakes add questions that must then be answered without mistakes.</summary>
    public bool HasAddOnQuestions => ExtraQuestionsPerMistake > 0;

    private TestRules(int questionCount, TimeSpan timeLimit, int maxMistakes, int extraQuestionsPerMistake, TimeSpan extraTimePerMistake)
    {
        QuestionCount = questionCount;
        TimeLimit = timeLimit;
        MaxMistakes = maxMistakes;
        ExtraQuestionsPerMistake = extraQuestionsPerMistake;
        ExtraTimePerMistake = extraTimePerMistake;
    }

    /// <summary>Validates and creates test rules.</summary>
    /// <param name="questionCount">From 1 to <see cref="MaxQuestionCount"/>.</param>
    /// <param name="timeLimit">From <see cref="MinTimeLimit"/> to <see cref="MaxTimeLimit"/>.</param>
    /// <param name="maxMistakes">From 0 to one less than <paramref name="questionCount"/>.</param>
    /// <param name="extraQuestionsPerMistake">From 0 to <see cref="MaxExtraQuestionsPerMistake"/>.</param>
    /// <param name="extraTimePerMistake">From zero to <see cref="MaxExtraTimePerMistake"/>; must be zero when no questions are added.</param>
    /// <returns>The rules, or the first validation failure encountered.</returns>
    public static Result<TestRules> Create(
        int questionCount, TimeSpan timeLimit, int maxMistakes, int extraQuestionsPerMistake, TimeSpan extraTimePerMistake)
    {
        if (questionCount is < 1 or > MaxQuestionCount)
            return Result.Failure<TestRules>(DomainErrors.TestRules.QuestionCountOutOfRange);

        if (timeLimit < MinTimeLimit || timeLimit > MaxTimeLimit)
            return Result.Failure<TestRules>(DomainErrors.TestRules.TimeLimitOutOfRange);

        if (maxMistakes < 0 || maxMistakes >= questionCount)
            return Result.Failure<TestRules>(DomainErrors.TestRules.MaxMistakesOutOfRange);

        if (extraQuestionsPerMistake is < 0 or > MaxExtraQuestionsPerMistake)
            return Result.Failure<TestRules>(DomainErrors.TestRules.ExtraQuestionsOutOfRange);

        if (extraTimePerMistake < TimeSpan.Zero || extraTimePerMistake > MaxExtraTimePerMistake)
            return Result.Failure<TestRules>(DomainErrors.TestRules.ExtraTimeOutOfRange);

        if (extraQuestionsPerMistake == 0 && extraTimePerMistake > TimeSpan.Zero)
            return Result.Failure<TestRules>(DomainErrors.TestRules.ExtraTimeWithoutExtraQuestions);

        return new TestRules(questionCount, timeLimit, maxMistakes, extraQuestionsPerMistake, extraTimePerMistake);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return QuestionCount;
        yield return TimeLimit;
        yield return MaxMistakes;
        yield return ExtraQuestionsPerMistake;
        yield return ExtraTimePerMistake;
    }
}
