using AutoElite.Application.Users;

namespace AutoElite.Application.Leads;

/// <summary>
/// Checks that a manager can take on one more lead in progress: each manager works on no more leads at a time than the
/// limit that an administrator sets for them. An administrator may give a manager leads beyond the limit.
/// </summary>
internal sealed class LeadCapacity(IUserRepository userRepository, ILeadQueries leadQueries, ICurrentUser currentUser)
{
    /// <summary>Checks that the manager works on fewer leads than their limit, unless the caller is an administrator.</summary>
    /// <param name="managerId">The identifier of the manager.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid managerId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.Administrator))
            return Result.Success();

        var manager = await userRepository.GetByIdAsync(managerId, cancellationToken);
        var limit = manager?.LeadLimit ?? User.DefaultLeadLimit;
        var inProgress = await leadQueries.CountInProgressAsync(managerId, cancellationToken);

        return inProgress < limit ? Result.Success() : Result.Failure(ApplicationErrors.Lead.ManagerAtLimit);
    }
}
