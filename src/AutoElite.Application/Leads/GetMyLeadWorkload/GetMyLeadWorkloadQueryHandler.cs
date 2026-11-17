using AutoElite.Application.Users;

namespace AutoElite.Application.Leads.GetMyLeadWorkload;

/// <summary>Counts the leads in progress of the user the request runs for and reads their limit.</summary>
internal sealed class GetMyLeadWorkloadQueryHandler(
    IUserRepository userRepository,
    ILeadQueries leadQueries,
    ICurrentUser currentUser) : IQueryHandler<GetMyLeadWorkloadQuery, LeadWorkload>
{
    /// <inheritdoc />
    public async Task<Result<LeadWorkload>> Handle(GetMyLeadWorkloadQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<LeadWorkload>(ApplicationErrors.NotFound<User>());

        var inProgress = await leadQueries.CountInProgressAsync(userId, cancellationToken);

        return new LeadWorkload(inProgress, user.LeadLimit);
    }
}
