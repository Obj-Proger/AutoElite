using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>
/// Grants a user a single <see cref="UserRole"/>. A user holding several roles has one assignment per role.
/// </summary>
/// <remarks>
/// Assignments are immutable: changing a role means removing one assignment and creating another.
/// Uniqueness of the (user, role) pair, the "at least one administrator" rule, and the active-user
/// check span multiple records, so they are enforced outside this entity: by the application layer
/// and by a unique database index.
/// </remarks>
public sealed class UserRoleAssignment : BaseEntity
{
    /// <summary>Gets the identifier of the user holding the role.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the granted role.</summary>
    public UserRole Role { get; private set; }

    private UserRoleAssignment(Guid id, Guid userId, UserRole role) : base(id)
    {
        UserId = userId;
        Role = role;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private UserRoleAssignment()
    {
    }

    /// <summary>Creates a role assignment for the specified user.</summary>
    /// <param name="userId">The identifier of the user receiving the role.</param>
    /// <param name="role">The role to grant.</param>
    /// <returns>The created assignment, or a failure if the user ID is empty or the role is not defined.</returns>
    public static Result<UserRoleAssignment> Create(Guid userId, UserRole role)
    {
        var userIdResult = Guard.NotEmpty(userId, DomainErrors.UserRoleAssignment.UserIdEmpty);
        if (userIdResult.IsFailure)
            return Result.Failure<UserRoleAssignment>(userIdResult.Error);

        var roleResult = Guard.DefinedEnum<UserRole>(role, DomainErrors.UserRoleAssignment.RoleNotDefined);
        if (roleResult.IsFailure)
            return Result.Failure<UserRoleAssignment>(roleResult.Error);

        return new UserRoleAssignment(Guid.CreateVersion7(), userId, role);
    }
}