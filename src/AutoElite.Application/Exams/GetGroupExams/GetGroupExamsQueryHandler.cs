namespace AutoElite.Application.Exams.GetGroupExams;

/// <summary>Reads the exams and hides groups the caller may not see as not found.</summary>
internal sealed class GetGroupExamsQueryHandler(IExamQueries examQueries, ICurrentUser currentUser)
    : IQueryHandler<GetGroupExamsQuery, IReadOnlyList<ExamItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExamItem>>> Handle(GetGroupExamsQuery query, CancellationToken cancellationToken)
    {
        var group = await examQueries.GetOfGroupAsync(query.GroupId, cancellationToken);
        if (group is null || !ExamReaders.MayRead(currentUser, group.BranchId, group.ResponsibleInstructorId))
            return Result.Failure<IReadOnlyList<ExamItem>>(ApplicationErrors.NotFound<StudyGroup>());

        return Result.Success(group.Exams);
    }
}
