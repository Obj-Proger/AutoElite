using AutoElite.Application.Users;

namespace AutoElite.Application.Leads;

/// <summary>Checks that a user can be made responsible for a lead.</summary>
internal sealed class LeadManagers(StaffMembers staffMembers)
{
    /// <summary>
    /// Checks that the user is an active manager who works either for the whole school or for the lead's branch.
    /// </summary>
    /// <param name="managerId">The identifier of the user.</param>
    /// <param name="leadBranchId">The branch of the lead, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid managerId, Guid? leadBranchId, CancellationToken cancellationToken) =>
        await staffMembers.CheckAsync(managerId, UserRole.Manager, leadBranchId, cancellationToken) switch
        {
            StaffCheck.Suitable => Result.Success(),
            StaffCheck.OfAnotherBranch => Result.Failure(ApplicationErrors.Lead.ManagerOfAnotherBranch),
            _ => Result.Failure(ApplicationErrors.Lead.NotAManager)
        };
}
