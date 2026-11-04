namespace AutoElite.Application.Exams.GetMyExams;

/// <summary>Reads the caller's exams.</summary>
internal sealed class GetMyExamsQueryHandler(IExamQueries examQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyExamsQuery, IReadOnlyList<StudentExamItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StudentExamItem>>> Handle(GetMyExamsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await examQueries.GetOfStudentAsync(currentUser.GetRequiredUserId(), cancellationToken));
}
