using AutoElite.Domain.Policies;

namespace AutoElite.Application.Users.GrantRole;

/// <summary>Checks the role compatibility policy and adds the assignment.</summary>
internal sealed class GrantRoleCommandHandler(ManagedUsers managedUsers, IUserRoleRepository userRoleRepository)
    : ICommandHandler<GrantRoleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(GrantRoleCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);
        if (managedResult.IsFailure)
            return Result.Failure(managedResult.Error);

        var (user, roles) = managedResult.Value;
        var policyResult = RoleAssignmentPolicy.CanAssign(user, roles, command.Role);
        if (policyResult.IsFailure)
            return policyResult;

        var assignmentResult = UserRoleAssignment.Create(user.Id, command.Role);
        if (assignmentResult.IsFailure)
            return Result.Failure(assignmentResult.Error);

        userRoleRepository.Add(assignmentResult.Value);

        return Result.Success();
    }
}
