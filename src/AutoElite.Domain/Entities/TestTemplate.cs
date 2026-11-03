using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A test template set up by a theory instructor (FR-7.4): the licence category and source of the questions, the blocks
/// a test is drawn in, and the <see cref="TestRules"/> it is taken by.
/// </summary>
/// <remarks>
/// <para>
/// The blocks together draw exactly <see cref="TestRules.QuestionCount"/> questions. Their topics do not overlap, so
/// every question belongs to one block and added questions come from the block of the mistake. A block that covers
/// all topics must be the only one.
/// </para>
/// <para>Templates used by past sessions or exams are deactivated rather than deleted.</para>
/// </remarks>
public sealed class TestTemplate : AuditableEntity
{
    /// <summary>The most blocks a template may have.</summary>
    public const int MaxBlocks = 10;

    private const int MaxNameLength = 200;

    private readonly List<TestBlock> _blocks = [];

    /// <summary>Gets the template name, for example "Exam ticket, category B".</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the licence category of the questions.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the rules the test is taken by.</summary>
    public TestRules Rules { get; private set; } = null!;

    /// <summary>Gets the blocks the questions are drawn in, in the order they are shown.</summary>
    public IReadOnlyList<TestBlock> Blocks => _blocks.AsReadOnly();

    /// <summary>Gets the source questions are drawn from, or <see langword="null"/> for questions of any source.</summary>
    public QuestionSource? Source { get; private set; }

    /// <summary>Gets a value indicating whether the template can be used for new tests.</summary>
    public bool IsActive { get; private set; }

    private TestTemplate(Guid id) : base(id)
    {
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private TestTemplate()
    {
    }

    /// <summary>Creates a template.</summary>
    /// <param name="name">The name. Required.</param>
    /// <param name="category">The licence category of the questions.</param>
    /// <param name="rules">The validated rules.</param>
    /// <param name="blocks">From 1 to <see cref="MaxBlocks"/> blocks with non-overlapping topics that draw <see cref="TestRules.QuestionCount"/> questions together.</param>
    /// <param name="source">The source to draw from, or <see langword="null"/> for any.</param>
    /// <returns>The template, or the first validation failure encountered.</returns>
    public static Result<TestTemplate> Create(
        string? name, LicenseCategory category, TestRules rules, IReadOnlyList<TestBlock> blocks, QuestionSource? source)
    {
        var template = new TestTemplate(Guid.CreateVersion7());

        var result = template.Apply(name, category, rules, blocks, source);
        if (result.IsFailure)
            return Result.Failure<TestTemplate>(result.Error);

        return template;
    }

    /// <summary>Replaces the template settings. The rules are the same as in <see cref="Create"/>.</summary>
    /// <returns>A failure if the template is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(
        string? name, LicenseCategory category, TestRules rules, IReadOnlyList<TestBlock> blocks, QuestionSource? source)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.TestTemplate.CannotModifyInactive);

        return Apply(name, category, rules, blocks, source);
    }

    /// <summary>Stops using the template for new tests.</summary>
    /// <returns>A failure if the template is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.TestTemplate.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Allows the template to be used again.</summary>
    /// <returns>A failure if the template is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.TestTemplate.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private Result Apply(
        string? name, LicenseCategory category, TestRules rules, IReadOnlyList<TestBlock> blocks, QuestionSource? source)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.TestTemplate.NameEmpty, DomainErrors.TestTemplate.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var categoryResult = Guard.DefinedEnum(category, DomainErrors.TestTemplate.CategoryNotDefined);
        if (categoryResult.IsFailure)
            return Result.Failure(categoryResult.Error);

        var blocksResult = ValidateBlocks(rules, blocks);
        if (blocksResult.IsFailure)
            return blocksResult;

        if (source is { } value && !Enum.IsDefined(value))
            return Result.Failure(DomainErrors.TestTemplate.SourceNotDefined);

        Name = nameResult.Value;
        Category = category;
        Rules = rules;
        Source = source;
        _blocks.Clear();
        _blocks.AddRange(blocks);

        return Result.Success();
    }

    private static Result ValidateBlocks(TestRules rules, IReadOnlyList<TestBlock> blocks)
    {
        if (blocks.Count == 0)
            return Result.Failure(DomainErrors.TestTemplate.NoBlocks);

        if (blocks.Count > MaxBlocks)
            return Result.Failure(DomainErrors.TestTemplate.TooManyBlocks);

        if (blocks.Count > 1 && blocks.Any(block => block.CoversAllTopics))
            return Result.Failure(DomainErrors.TestTemplate.AllTopicsBlockNotAlone);

        var topicIds = blocks.SelectMany(block => block.TopicIds).ToList();
        if (topicIds.Distinct().Count() != topicIds.Count)
            return Result.Failure(DomainErrors.TestTemplate.TopicInSeveralBlocks);

        if (blocks.Sum(block => block.QuestionCount) != rules.QuestionCount)
            return Result.Failure(DomainErrors.TestTemplate.BlocksDoNotMatchQuestionCount);

        return Result.Success();
    }
}
