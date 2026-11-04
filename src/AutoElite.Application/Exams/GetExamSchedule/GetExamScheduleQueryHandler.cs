namespace AutoElite.Application.Exams.GetExamSchedule;

/// <summary>Reads the schedule within the caller's scope, limited to their own groups for a theory instructor.</summary>
internal sealed class GetExamScheduleQueryHandler(IExamQueries examQueries, ICurrentUser currentUser)
    : IQueryHandler<GetExamScheduleQuery, IReadOnlyList<ExamItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExamItem>>> Handle(GetExamScheduleQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<ExamItem>>(periodResult.Error);

        Guid? responsibleInstructorId = currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor)
            ? null
            : currentUser.GetRequiredUserId();
        var filter = new ExamScheduleFilter(periodResult.Value, query.Kind, query.GroupId, responsibleInstructorId);

        return Result.Success(await examQueries.GetScheduleAsync(filter, DataScope.For(currentUser), cancellationToken));
    }
}
