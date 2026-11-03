using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Policies;

/// <summary>
/// Rules that span all role assignments of a user, or all administrators of the school, and therefore
/// cannot be checked by a single <see cref="UserRoleAssignment"/>.
/// </summary>
/// <remarks>
/// The caller loads the current state (the user's roles, the number of active administrators) and must
/// keep it from changing until the result is saved; otherwise two concurrent requests could both pass.
/// </remarks>
public static class RoleAssignmentPolicy
{
    /// <summary>Checks whether <paramref name="role"/> can be granted to <paramref name="user"/>.</summary>
    /// <param name="user">The user receiving the role.</param>
    /// <param name="currentRoles">The roles the user already holds.</param>
    /// <param name="role">The role to grant.</param>
    /// <returns>
    /// A failure if the user is inactive, the role is not defined, the user already holds it, or it cannot be
    /// combined with a role the user holds: a student cannot be an instructor at the same time.
    /// </returns>
    public static Result CanAssign(User user, IReadOnlyCollection<UserRole> currentRoles, UserRole role)
    {
        if (!user.IsActive)
            return Result.Failure(DomainErrors.UserRoleAssignment.UserInactive);

        var roleResult = Guard.DefinedEnum(role, DomainErrors.UserRoleAssignment.RoleNotDefined);
        if (roleResult.IsFailure)
            return Result.Failure(roleResult.Error);

        if (currentRoles.Contains(role))
            return Result.Failure(DomainErrors.UserRoleAssignment.AlreadyAssigned);

        if (AreIncompatible(role, currentRoles))
            return Result.Failure(DomainErrors.UserRoleAssignment.IncompatibleRoles);

        return Result.Success();
    }

    /// <summary>Checks whether <paramref name="role"/> can be taken away from a user.</summary>
    /// <param name="currentRoles">The roles the user holds.</param>
    /// <param name="role">The role to take away.</param>
    /// <param name="activeAdministratorCount">The number of active users in the school who hold the administrator role.</param>
    /// <returns>A failure if the user does not hold the role, or it is the role of the last active administrator.</returns>
    public static Result CanRevoke(IReadOnlyCollection<UserRole> currentRoles, UserRole role, int activeAdministratorCount)
    {
        if (!currentRoles.Contains(role))
            return Result.Failure(DomainErrors.UserRoleAssignment.NotAssigned);

        if (role == UserRole.Administrator && activeAdministratorCount <= 1)
            return Result.Failure(DomainErrors.UserRoleAssignment.LastAdministrator);

        return Result.Success();
    }

    private static bool AreIncompatible(UserRole role, IReadOnlyCollection<UserRole> currentRoles) =>
        role == UserRole.Student
            ? currentRoles.Any(IsInstructor)
            : IsInstructor(role) && currentRoles.Contains(UserRole.Student);

    private static bool IsInstructor(UserRole role) =>
        role is UserRole.DrivingInstructor or UserRole.TheoryInstructor;
}
