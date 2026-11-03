using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A training course from the school's catalog: the licence category it prepares for, the price, the number of
/// theory and practice hours, and the curriculum as an ordered list of topics.
/// </summary>
/// <remarks>
/// <para>
/// A contract copies the price when it is drawn up, so changing the price does not affect existing contracts.
/// Courses are deactivated rather than deleted; an inactive course cannot be modified.
/// </para>
/// <para>
/// Topic positions are always 1..n without gaps. Whether a topic may be removed (questions or lessons may refer to
/// it) is checked by the application layer.
/// </para>
/// </remarks>
public sealed class Course : AuditableEntity
{
    /// <summary>The largest number of theory or practice hours a course may have.</summary>
    public const int MaxHours = 1000;

    private const int MaxNameLength = 200;
    private const int MaxDescriptionLength = 2000;

    private readonly List<CourseTopic> _topics = [];

    /// <summary>Gets the course name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the licence category the course prepares for.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the current price of the course.</summary>
    public Money Price { get; private set; } = null!;

    /// <summary>Gets the number of theory hours.</summary>
    public int TheoryHours { get; private set; }

    /// <summary>Gets the number of practical driving hours.</summary>
    public int PracticeHours { get; private set; }

    /// <summary>Gets the course description, if any.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the course is offered.
    /// Deactivated courses are kept for history but cannot be modified.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the curriculum topics ordered by position.</summary>
    public IReadOnlyList<CourseTopic> Topics => _topics.AsReadOnly();

    private Course(Guid id, CourseDetails details) : base(id)
    {
        Apply(details);
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Course()
    {
    }

    /// <summary>Creates a new active course without topics.</summary>
    /// <param name="name">The course name. Required.</param>
    /// <param name="category">The licence category the course prepares for.</param>
    /// <param name="price">The price; must be greater than zero.</param>
    /// <param name="theoryHours">Theory hours, from 0 to <see cref="MaxHours"/>.</param>
    /// <param name="practiceHours">Practice hours, from 0 to <see cref="MaxHours"/>.</param>
    /// <param name="description">An optional description.</param>
    /// <returns>The created course, or the first validation failure encountered.</returns>
    public static Result<Course> Create(
        string? name, LicenseCategory category, Money price, int theoryHours, int practiceHours, string? description)
    {
        var detailsResult = ValidateDetails(name, category, price, theoryHours, practiceHours, description);
        if (detailsResult.IsFailure)
            return Result.Failure<Course>(detailsResult.Error);

        return new Course(Guid.CreateVersion7(), detailsResult.Value);
    }

    /// <summary>Replaces the course details. The rules are the same as in <see cref="Create"/>.</summary>
    /// <returns>A failure if the course is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(
        string? name, LicenseCategory category, Money price, int theoryHours, int practiceHours, string? description)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Course.CannotModifyInactive);

        var detailsResult = ValidateDetails(name, category, price, theoryHours, practiceHours, description);
        if (detailsResult.IsFailure)
            return Result.Failure(detailsResult.Error);

        Apply(detailsResult.Value);

        return Result.Success();
    }

    /// <summary>Deactivates the course so that it is no longer offered.</summary>
    /// <returns>A failure if the course is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Course.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Reactivates a previously deactivated course.</summary>
    /// <returns>A failure if the course is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.Course.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    /// <summary>Adds a topic at the end of the curriculum.</summary>
    /// <returns>
    /// The added topic, or a failure if the course is inactive, the title or description is invalid,
    /// or another topic has the same title.
    /// </returns>
    public Result<CourseTopic> AddTopic(string? title, string? description)
    {
        if (!IsActive)
            return Result.Failure<CourseTopic>(DomainErrors.Course.CannotModifyInactive);

        var valuesResult = CourseTopic.Validate(title, description);
        if (valuesResult.IsFailure)
            return Result.Failure<CourseTopic>(valuesResult.Error);

        var (validTitle, validDescription) = valuesResult.Value;

        if (HasTopicTitled(validTitle, exceptTopicId: null))
            return Result.Failure<CourseTopic>(DomainErrors.Course.DuplicateTopicTitle);

        var topic = CourseTopic.Create(validTitle, validDescription, _topics.Count + 1);
        _topics.Add(topic);

        return topic;
    }

    /// <summary>Changes the title and description of a topic.</summary>
    /// <returns>
    /// A failure if the course is inactive, the topic does not belong to the course, the values are invalid,
    /// or another topic has the same title.
    /// </returns>
    public Result UpdateTopic(Guid topicId, string? title, string? description)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Course.CannotModifyInactive);

        var topic = FindTopic(topicId);
        if (topic is null)
            return Result.Failure(DomainErrors.Course.TopicNotFound);

        var valuesResult = CourseTopic.Validate(title, description);
        if (valuesResult.IsFailure)
            return Result.Failure(valuesResult.Error);

        var (validTitle, validDescription) = valuesResult.Value;

        if (HasTopicTitled(validTitle, exceptTopicId: topicId))
            return Result.Failure(DomainErrors.Course.DuplicateTopicTitle);

        topic.Update(validTitle, validDescription);

        return Result.Success();
    }

    /// <summary>Moves a topic to another position, shifting the topics in between.</summary>
    /// <param name="topicId">The topic to move.</param>
    /// <param name="newPosition">The one-based target position, from 1 to the number of topics.</param>
    /// <returns>A failure if the course is inactive, the topic does not belong to the course, or the position is out of range.</returns>
    public Result MoveTopic(Guid topicId, int newPosition)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Course.CannotModifyInactive);

        var topic = FindTopic(topicId);
        if (topic is null)
            return Result.Failure(DomainErrors.Course.TopicNotFound);

        if (newPosition < 1 || newPosition > _topics.Count)
            return Result.Failure(DomainErrors.Course.TopicPositionOutOfRange);

        _topics.Remove(topic);
        _topics.Insert(newPosition - 1, topic);
        Renumber();

        return Result.Success();
    }

    /// <summary>Removes a topic and closes the gap in the positions.</summary>
    /// <returns>A failure if the course is inactive or the topic does not belong to the course.</returns>
    public Result RemoveTopic(Guid topicId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Course.CannotModifyInactive);

        var topic = FindTopic(topicId);
        if (topic is null)
            return Result.Failure(DomainErrors.Course.TopicNotFound);

        _topics.Remove(topic);
        Renumber();

        return Result.Success();
    }

    private CourseTopic? FindTopic(Guid topicId) => _topics.Find(topic => topic.Id == topicId);

    private bool HasTopicTitled(string title, Guid? exceptTopicId) =>
        _topics.Exists(topic => topic.Id != exceptTopicId && string.Equals(topic.Title, title, StringComparison.OrdinalIgnoreCase));

    private void Renumber()
    {
        for (var index = 0; index < _topics.Count; index++)
            _topics[index].MoveTo(index + 1);
    }

    private void Apply(CourseDetails details)
    {
        Name = details.Name;
        Category = details.Category;
        Price = details.Price;
        TheoryHours = details.TheoryHours;
        PracticeHours = details.PracticeHours;
        Description = details.Description;
    }

    private static Result<CourseDetails> ValidateDetails(
        string? name, LicenseCategory category, Money price, int theoryHours, int practiceHours, string? description)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.Course.NameEmpty, DomainErrors.Course.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure<CourseDetails>(nameResult.Error);

        var categoryResult = Guard.DefinedEnum(category, DomainErrors.Course.CategoryNotDefined);
        if (categoryResult.IsFailure)
            return Result.Failure<CourseDetails>(categoryResult.Error);

        if (!price.IsPositive)
            return Result.Failure<CourseDetails>(DomainErrors.Course.PriceNotPositive);

        if (theoryHours is < 0 or > MaxHours || practiceHours is < 0 or > MaxHours)
            return Result.Failure<CourseDetails>(DomainErrors.Course.HoursOutOfRange);

        if (theoryHours == 0 && practiceHours == 0)
            return Result.Failure<CourseDetails>(DomainErrors.Course.NoHours);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.Course.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<CourseDetails>(descriptionResult.Error);

        return new CourseDetails(nameResult.Value, category, price, theoryHours, practiceHours, descriptionResult.Value);
    }

    /// <summary>Validated course details, passed between validation and assignment.</summary>
    private sealed record CourseDetails(
        string Name, LicenseCategory Category, Money Price, int TheoryHours, int PracticeHours, string? Description);
}
