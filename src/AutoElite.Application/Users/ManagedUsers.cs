namespace AutoElite.Application.Users;

/// <summary>
/// Loads a user for a staff action and checks that the caller may act on the user: administrators act on anyone,
/// managers only on students of their branch.
/// </summary>
internal sealed class ManagedUsers(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the user if the caller may act on it.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>
    /// The user and its roles; a not found error if the user does not exist or is outside the caller's scope, or a
    /// forbidden error if a manager acts on a user who holds a staff role.
    /// </returns>
    public async Task<Result<ManagedUser>> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !DataScope.For(currentUser).Includes(user.BranchId))
            return Result.Failure<ManagedUser>(ApplicationErrors.NotFound<User>());

        var roles = await userRoleRepository.GetRolesAsync(user.Id, cancellationToken);

        if (!currentUser.IsInRole(UserRole.Administrator) && roles.Any(role => role != UserRole.Student))
            return Result.Failure<ManagedUser>(ApplicationErrors.Account.ManagerManagesOnlyStudents);

        return new ManagedUser(user, roles);
    }
}
