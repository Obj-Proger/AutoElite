namespace AutoElite.Application.Users;

/// <summary>The outcome of checking that a user can take on a staff duty.</summary>
internal enum StaffCheck
{
    /// <summary>The user is active, holds the role and may work with the branch.</summary>
    Suitable = 1,

    /// <summary>The user does not exist, is inactive or does not hold the role.</summary>
    NotInRole = 2,

    /// <summary>The user works for another branch.</summary>
    OfAnotherBranch = 3
}

/// <summary>Checks that a user can be made responsible for a record, such as a lead or a study group.</summary>
internal sealed class StaffMembers(IUserRepository userRepository, IUserRoleRepository userRoleRepository)
{
    /// <summary>
    /// Checks that the user is active, holds the role and works either for the whole school or for the record's branch.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="role">The role the duty needs.</param>
    /// <param name="branchId">The branch of the record, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<StaffCheck> CheckAsync(Guid userId, UserRole role, Guid? branchId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not { IsActive: true })
            return StaffCheck.NotInRole;

        var roles = await userRoleRepository.GetRolesAsync(user.Id, cancellationToken);
        if (!roles.Contains(role))
            return StaffCheck.NotInRole;

        return user.BranchId is { } userBranchId && branchId is { } recordBranchId && userBranchId != recordBranchId
            ? StaffCheck.OfAnotherBranch
            : StaffCheck.Suitable;
    }
}
