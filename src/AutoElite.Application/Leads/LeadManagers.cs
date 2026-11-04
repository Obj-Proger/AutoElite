using AutoElite.Application.Users;

namespace AutoElite.Application.Leads;

/// <summary>Checks that a user can be made responsible for a lead.</summary>
internal sealed class LeadManagers(IUserRepository userRepository, IUserRoleRepository userRoleRepository)
{
    /// <summary>
    /// Checks that the user is an active manager who works either for the whole school or for the lead's branch.
    /// </summary>
    /// <param name="managerId">The identifier of the user.</param>
    /// <param name="leadBranchId">The branch of the lead, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid managerId, Guid? leadBranchId, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(managerId, cancellationToken);
        if (manager is not { IsActive: true })
            return Result.Failure(ApplicationErrors.Lead.NotAManager);

        var roles = await userRoleRepository.GetRolesAsync(manager.Id, cancellationToken);
        if (!roles.Contains(UserRole.Manager))
            return Result.Failure(ApplicationErrors.Lead.NotAManager);

        return manager.BranchId is { } managerBranchId && leadBranchId is { } branchId && managerBranchId != branchId
            ? Result.Failure(ApplicationErrors.Lead.ManagerOfAnotherBranch)
            : Result.Success();
    }
}
