namespace AutoElite.Application.StaffTasks.GetStaffTask;

/// <summary>Reads the card and hides a task the caller may not see as not found.</summary>
internal sealed class GetStaffTaskQueryHandler(IStaffTaskQueries taskQueries, ICurrentUser currentUser)
    : IQueryHandler<GetStaffTaskQuery, StaffTaskDetails>
{
    /// <inheritdoc />
    public async Task<Result<StaffTaskDetails>> Handle(GetStaffTaskQuery query, CancellationToken cancellationToken)
    {
        var details = await taskQueries.GetDetailsAsync(query.TaskId, cancellationToken);

        return details is not null && CanSee(details)
            ? details
            : Result.Failure<StaffTaskDetails>(ApplicationErrors.NotFound<StaffTask>());
    }

    private bool CanSee(StaffTaskDetails details) =>
        details.AssigneeId == currentUser.UserId
        || currentUser.IsInRole(UserRole.Administrator)
        || (currentUser.IsInRole(UserRole.Manager) && DataScope.For(currentUser).Includes(details.AssigneeBranchId));
}
