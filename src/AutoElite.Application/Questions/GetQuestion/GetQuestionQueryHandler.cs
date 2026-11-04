namespace AutoElite.Application.Questions.GetQuestion;

/// <summary>Reads the card.</summary>
internal sealed class GetQuestionQueryHandler(IQuestionQueries questionQueries) : IQueryHandler<GetQuestionQuery, QuestionDetails>
{
    /// <inheritdoc />
    public async Task<Result<QuestionDetails>> Handle(GetQuestionQuery query, CancellationToken cancellationToken)
    {
        var details = await questionQueries.GetDetailsAsync(query.QuestionId, cancellationToken);

        return details is null ? Result.Failure<QuestionDetails>(ApplicationErrors.NotFound<Question>()) : details;
    }
}
