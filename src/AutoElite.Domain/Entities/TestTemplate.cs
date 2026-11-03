using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A test template set up by a theory instructor: which questions a test draws (licence category, optionally limited
/// to some course topics and to one source) and the <see cref="TestRules"/> it is taken by (FR-7.4).
/// </summary>
/// <remarks>Templates used by past sessions or exams are deactivated rather than deleted.</remarks>
public sealed class TestTemplate : AuditableEntity
{
    private const int MaxNameLength = 200;

    private readonly List<Guid> _topicIds = [];

    /// <summary>Gets the template name, for example "Exam ticket, category B".</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the licence category of the questions.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the rules the test is taken by.</summary>
    public TestRules Rules { get; private set; } = null!;

    /// <summary>Gets the course topics questions are drawn from; empty means all topics.</summary>
    public IReadOnlyList<Guid> TopicIds => _topicIds.AsReadOnly();

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
    /// <param name="topicIds">The course topics to draw from, without repetitions; empty for all topics.</param>
    /// <param name="source">The source to draw from, or <see langword="null"/> for any.</param>
    /// <returns>The template, or the first validation failure encountered.</returns>
    public static Result<TestTemplate> Create(
        string? name, LicenseCategory category, TestRules rules, IReadOnlyCollection<Guid> topicIds, QuestionSource? source)
    {
        var template = new TestTemplate(Guid.CreateVersion7());

        var result = template.Apply(name, category, rules, topicIds, source);
        if (result.IsFailure)
            return Result.Failure<TestTemplate>(result.Error);

        return template;
    }

    /// <summary>Replaces the template settings. The rules are the same as in <see cref="Create"/>.</summary>
    /// <returns>A failure if the template is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(
        string? name, LicenseCategory category, TestRules rules, IReadOnlyCollection<Guid> topicIds, QuestionSource? source)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.TestTemplate.CannotModifyInactive);

        return Apply(name, category, rules, topicIds, source);
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
        string? name, LicenseCategory category, TestRules rules, IReadOnlyCollection<Guid> topicIds, QuestionSource? source)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.TestTemplate.NameEmpty, DomainErrors.TestTemplate.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var categoryResult = Guard.DefinedEnum(category, DomainErrors.TestTemplate.CategoryNotDefined);
        if (categoryResult.IsFailure)
            return Result.Failure(categoryResult.Error);

        if (topicIds.Contains(Guid.Empty))
            return Result.Failure(DomainErrors.TestTemplate.TopicIdEmpty);

        if (topicIds.Distinct().Count() != topicIds.Count)
            return Result.Failure(DomainErrors.TestTemplate.DuplicateTopic);

        if (source is { } value && !Enum.IsDefined(value))
            return Result.Failure(DomainErrors.TestTemplate.SourceNotDefined);

        Name = nameResult.Value;
        Category = category;
        Rules = rules;
        Source = source;
        _topicIds.Clear();
        _topicIds.AddRange(topicIds);

        return Result.Success();
    }
}
