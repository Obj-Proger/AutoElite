namespace AutoElite.Application.Exams.GetExam;

/// <summary>Reads the card and hides exams the caller may not see as not found.</summary>
internal sealed class GetExamQueryHandler(IExamQueries examQueries, ICurrentUser currentUser) : IQueryHandler<GetExamQuery, ExamDetails>
{
    /// <inheritdoc />
    public async Task<Result<ExamDetails>> Handle(GetExamQuery query, CancellationToken cancellationToken)
    {
        var details = await examQueries.GetDetailsAsync(query.ExamId, cancellationToken);

        return details is not null && ExamReaders.MayRead(currentUser, details.Exam.BranchId, details.Exam.ResponsibleInstructorId)
            ? details
            : Result.Failure<ExamDetails>(ApplicationErrors.NotFound<Exam>());
    }
}
