namespace AutoElite.Domain.Entities;

/// <summary>
/// A driving route prepared by a driving instructor, for example an exam route through the city.
/// </summary>
/// <remarks>
/// The author is fixed when the route is created; whether another instructor may edit it is decided by the
/// application layer. A route that is no longer used is deactivated rather than deleted.
/// </remarks>
public sealed class DrivingRoute : AuditableEntity
{
    private const int MaxNameLength = 200;
    private const int MaxDescriptionLength = 4000;

    /// <summary>Gets the name of the route.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the description of the route: the streets, manoeuvres and points to practise.</summary>
    public string Description { get; private set; } = null!;

    /// <summary>Gets the identifier of the driving instructor who created the route.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Gets the identifier of the training ground the route starts from, or <see langword="null"/> if none.</summary>
    public Guid? TrainingGroundId { get; private set; }

    /// <summary>Gets a value indicating whether the route is in use.</summary>
    public bool IsActive { get; private set; }

    private DrivingRoute(Guid id, string name, string description, Guid authorId, Guid? trainingGroundId) : base(id)
    {
        Name = name;
        Description = description;
        AuthorId = authorId;
        TrainingGroundId = trainingGroundId;
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private DrivingRoute()
    {
    }

    /// <summary>Creates a route.</summary>
    /// <param name="name">The name. Required.</param>
    /// <param name="description">The description of the route. Required.</param>
    /// <param name="authorId">The driving instructor creating the route; checking the role is up to the caller.</param>
    /// <param name="trainingGroundId">The training ground the route starts from, if any.</param>
    /// <returns>The route, or the first validation failure encountered.</returns>
    public static Result<DrivingRoute> Create(string? name, string? description, Guid authorId, Guid? trainingGroundId)
    {
        var validation = Validate(name, description, trainingGroundId);
        if (validation.IsFailure)
            return Result.Failure<DrivingRoute>(validation.Error);

        var authorResult = Guard.NotEmpty(authorId, DomainErrors.DrivingRoute.AuthorIdEmpty);
        if (authorResult.IsFailure)
            return Result.Failure<DrivingRoute>(authorResult.Error);

        var (validName, validDescription) = validation.Value;

        return new DrivingRoute(Guid.CreateVersion7(), validName, validDescription, authorId, trainingGroundId);
    }

    /// <summary>Replaces the name, the description and the starting ground. The author does not change.</summary>
    /// <returns>A failure if the route is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(string? name, string? description, Guid? trainingGroundId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.DrivingRoute.CannotModifyInactive);

        var validation = Validate(name, description, trainingGroundId);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        (Name, Description) = validation.Value;
        TrainingGroundId = trainingGroundId;

        return Result.Success();
    }

    /// <summary>Stops using the route.</summary>
    /// <returns>A failure if the route is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.DrivingRoute.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Starts using the route again.</summary>
    /// <returns>A failure if the route is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.DrivingRoute.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private static Result<(string Name, string Description)> Validate(string? name, string? description, Guid? trainingGroundId)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.DrivingRoute.NameEmpty, DomainErrors.DrivingRoute.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure<(string, string)>(nameResult.Error);

        var descriptionResult = Guard.RequiredText(description, MaxDescriptionLength, DomainErrors.DrivingRoute.DescriptionEmpty, DomainErrors.DrivingRoute.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<(string, string)>(descriptionResult.Error);

        if (trainingGroundId is { } groundId && groundId == Guid.Empty)
            return Result.Failure<(string, string)>(DomainErrors.DrivingRoute.TrainingGroundIdEmpty);

        return Result.Success((nameResult.Value, descriptionResult.Value));
    }
}
