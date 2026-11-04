namespace AutoElite.Application.DrivingSlots.GetDrivingSchedule;

/// <summary>
/// Reads the schedule within the caller's scope. A driving instructor who is not also staff of the office reads only
/// their own slots; asking for someone else's slots gives an empty schedule.
/// </summary>
internal sealed class GetDrivingScheduleQueryHandler(IDrivingSlotQueries slotQueries, ICurrentUser currentUser)
    : IQueryHandler<GetDrivingScheduleQuery, IReadOnlyList<DrivingScheduleItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DrivingScheduleItem>>> Handle(
        GetDrivingScheduleQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<DrivingScheduleItem>>(periodResult.Error);

        var instructorId = query.InstructorId;
        if (!currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager))
        {
            var ownId = currentUser.GetRequiredUserId();
            if (instructorId is { } requested && requested != ownId)
                return Result.Success<IReadOnlyList<DrivingScheduleItem>>([]);

            instructorId = ownId;
        }

        var filter = new DrivingScheduleFilter(periodResult.Value, instructorId, query.Status);

        return Result.Success(await slotQueries.GetScheduleAsync(filter, DataScope.For(currentUser), cancellationToken));
    }
}
