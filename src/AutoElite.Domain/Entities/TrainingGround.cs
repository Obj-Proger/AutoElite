using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>A training ground (a closed area or a starting point in the city) where practical lessons take place.</summary>
/// <remarks>A ground that is no longer used is deactivated rather than deleted, because past lessons refer to it.</remarks>
public sealed class TrainingGround : AuditableEntity
{
    /// <summary>The longest ground name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The longest ground description.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Gets the name of the ground.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the address of the ground.</summary>
    public Address Address { get; private set; } = null!;

    /// <summary>Gets the description (directions, what is available), if any.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the identifier of the branch the ground belongs to, or <see langword="null"/> if shared.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets a value indicating whether the ground is in use.</summary>
    public bool IsActive { get; private set; }

    private TrainingGround(Guid id, string name, Address address, string? description, Guid? branchId) : base(id)
    {
        Name = name;
        Address = address;
        Description = description;
        BranchId = branchId;
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private TrainingGround()
    {
    }

    /// <summary>Adds a training ground.</summary>
    /// <param name="name">The name. Required.</param>
    /// <param name="address">The validated address.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="branchId">The branch, if any.</param>
    /// <returns>The ground, or the first validation failure encountered.</returns>
    public static Result<TrainingGround> Create(string? name, Address address, string? description, Guid? branchId)
    {
        var validation = Validate(name, description, branchId);
        if (validation.IsFailure)
            return Result.Failure<TrainingGround>(validation.Error);

        var (validName, validDescription) = validation.Value;

        return new TrainingGround(Guid.CreateVersion7(), validName, address, validDescription, branchId);
    }

    /// <summary>Replaces the details of the ground. The rules are the same as in <see cref="Create"/>.</summary>
    /// <returns>A failure if the ground is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(string? name, Address address, string? description, Guid? branchId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.TrainingGround.CannotModifyInactive);

        var validation = Validate(name, description, branchId);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        (Name, Description) = validation.Value;
        Address = address;
        BranchId = branchId;

        return Result.Success();
    }

    /// <summary>Stops using the ground.</summary>
    /// <returns>A failure if the ground is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.TrainingGround.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Starts using the ground again.</summary>
    /// <returns>A failure if the ground is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.TrainingGround.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private static Result<(string Name, string? Description)> Validate(string? name, string? description, Guid? branchId)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.TrainingGround.NameEmpty, DomainErrors.TrainingGround.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure<(string, string?)>(nameResult.Error);

        var descriptionResult = Guard.OptionalText(description, MaxDescriptionLength, DomainErrors.TrainingGround.DescriptionTooLong);
        if (descriptionResult.IsFailure)
            return Result.Failure<(string, string?)>(descriptionResult.Error);

        if (branchId is { } branch && branch == Guid.Empty)
            return Result.Failure<(string, string?)>(DomainErrors.TrainingGround.BranchIdEmpty);

        return Result.Success((nameResult.Value, descriptionResult.Value));
    }
}
