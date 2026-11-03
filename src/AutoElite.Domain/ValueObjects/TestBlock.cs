namespace AutoElite.Domain.ValueObjects;

/// <summary>
/// One block of a test: how many questions it draws and from which course topics. The official exam has four blocks of
/// five questions; a mistake adds questions from the same block, and the rules may limit the mistakes per block.
/// </summary>
public sealed class TestBlock : ValueObject
{
    /// <summary>The most questions a block may draw.</summary>
    public const int MaxQuestionCount = 100;

    private readonly Guid[] _topicIds;

    /// <summary>Gets the number of questions the block draws.</summary>
    public int QuestionCount { get; }

    /// <summary>Gets the course topics the block draws from; empty means all topics.</summary>
    public IReadOnlyList<Guid> TopicIds => Array.AsReadOnly(_topicIds);

    /// <summary>Gets a value indicating whether the block draws from all topics.</summary>
    public bool CoversAllTopics => _topicIds.Length == 0;

    private TestBlock(int questionCount, Guid[] topicIds)
    {
        QuestionCount = questionCount;
        _topicIds = topicIds;
    }

    /// <summary>Validates and creates a block.</summary>
    /// <param name="questionCount">From 1 to <see cref="MaxQuestionCount"/>.</param>
    /// <param name="topicIds">The course topics without repetitions; empty for all topics.</param>
    /// <returns>The block, or the first validation failure encountered.</returns>
    public static Result<TestBlock> Create(int questionCount, IReadOnlyCollection<Guid> topicIds)
    {
        if (questionCount is < 1 or > MaxQuestionCount)
            return Result.Failure<TestBlock>(DomainErrors.TestBlock.QuestionCountOutOfRange);

        if (topicIds.Contains(Guid.Empty))
            return Result.Failure<TestBlock>(DomainErrors.TestBlock.TopicIdEmpty);

        if (topicIds.Distinct().Count() != topicIds.Count)
            return Result.Failure<TestBlock>(DomainErrors.TestBlock.DuplicateTopic);

        return new TestBlock(questionCount, [.. topicIds]);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return QuestionCount;

        foreach (var topicId in _topicIds)
            yield return topicId;
    }
}
