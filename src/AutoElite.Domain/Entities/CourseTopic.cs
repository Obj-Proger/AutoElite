namespace AutoElite.Domain.Entities;

/// <summary>A topic of a <see cref="Course"/>'s curriculum. Part of the course aggregate.</summary>
public sealed class CourseTopic : BaseEntity
{
    /// <summary>The longest topic title.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>The longest topic description.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Gets the title of the topic, unique within the course regardless of letter case.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Gets the description of the topic, if any.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the one-based position of the topic in the curriculum.</summary>
    public int Position { get; private set; }

    private CourseTopic(Guid id, string title, string? description, int position) : base(id)
    {
        Title = title;
        Description = description;
        Position = position;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private CourseTopic()
    {
    }

    /// <summary>Validates the title and description shared by creation and update.</summary>
    internal static Result<(string Title, string? Description)> Validate(string? title, string? description)
    {
        var titleResult = Guard.RequiredText(title, MaxTitleLength, DomainErrors.CourseTopic.TitleEmpty, DomainErrors.CourseTopic.TitleTooLong);
        if (titleResult.IsFailure)
            return Result.Failure<(string, string?)>(titleResult.Error);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.CourseTopic.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<(string, string?)>(descriptionResult.Error);

        return Result.Success((titleResult.Value, descriptionResult.Value));
    }

    /// <summary>Creates a topic from values already checked by <see cref="Validate"/>.</summary>
    internal static CourseTopic Create(string title, string? description, int position) =>
        new(Guid.CreateVersion7(), title, description, position);

    /// <summary>Replaces the title and description with values already checked by <see cref="Validate"/>.</summary>
    internal void Update(string title, string? description)
    {
        Title = title;
        Description = description;
    }

    /// <summary>Sets the position; the course keeps positions contiguous.</summary>
    internal void MoveTo(int position) => Position = position;
}
