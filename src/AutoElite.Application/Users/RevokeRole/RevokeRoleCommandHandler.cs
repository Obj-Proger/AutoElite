using AutoElite.Domain.Policies;

namespace AutoElite.Application.Users.RevokeRole;

/// <summary>Checks the revocation policy, counting the administrators under a lock, and removes the assignment.</summary>
internal sealed class RevokeRoleCommandHandler(ManagedUsers managedUsers, IUserRoleRepository userRoleRepository)
    : ICommandHandler<RevokeRoleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RevokeRoleCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);
        if (managedResult.IsFailure)
            return Result.Failure(managedResult.Error);

        var (user, roles) = managedResult.Value;
        var administratorCount = command.Role == UserRole.Administrator && user.IsActive
            ? await userRoleRepository.CountActiveAdministratorsAsync(cancellationToken)
            : int.MaxValue;

        var policyResult = RoleAssignmentPolicy.CanRevoke(roles, command.Role, administratorCount);
        if (policyResult.IsFailure)
            return policyResult;

        var assignment = await userRoleRepository.GetAsync(user.Id, command.Role, cancellationToken);
        if (assignment is null)
            return Result.Failure(DomainErrors.UserRoleAssignment.NotAssigned);

        userRoleRepository.Remove(assignment);

        return Result.Success();
    }
}
