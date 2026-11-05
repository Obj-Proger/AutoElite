using AutoElite.Application.Users;

namespace AutoElite.Application.StaffTasks;

/// <summary>Checks that a user can be given a task by the caller.</summary>
internal sealed class TaskAssignees(
    IUserRepository userRepository,
    IUserRoleRepository roleRepository,
    ICurrentUser currentUser)
{
    /// <summary>
    /// Checks that the user is an active staff member and, for a manager, works inside the manager's branch scope.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not { IsActive: true })
            return Result.Failure(ApplicationErrors.StaffTask.AssigneeNotStaff);

        var roles = await roleRepository.GetRolesAsync(user.Id, cancellationToken);
        if (!roles.Any(StaffTaskRoles.Staff.Contains))
            return Result.Failure(ApplicationErrors.StaffTask.AssigneeNotStaff);

        return DataScope.For(currentUser).Includes(user.BranchId)
            ? Result.Success()
            : Result.Failure(ApplicationErrors.StaffTask.AssigneeOfAnotherBranch);
    }
}
