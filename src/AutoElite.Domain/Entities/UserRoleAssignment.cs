using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>
/// Grants a user a single <see cref="UserRole"/>. A user holding several roles has one assignment per role.
/// </summary>
/// <remarks>
/// Assignments are immutable: changing a role means removing one assignment and creating another.
/// When and by whom the role was granted is recorded by the audit properties.
/// Rules that span several records (the user is active, the (user, role) pair is unique, a student is
/// not an instructor, at least one administrator remains) are defined in
/// <see cref="Policies.RoleAssignmentPolicy"/>; uniqueness is also enforced by a database index.
/// </remarks>
public sealed class UserRoleAssignment : AuditableEntity
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