namespace AutoElite.Application.Questions;

/// <summary>
/// Loads a question for a change. Official questions follow the traffic police exam and only administrators change
/// them; school questions are changed by administrators and theory instructors (FR-7.2).
/// </summary>
internal sealed class QuestionAccess(IQuestionRepository questionRepository, ICurrentUser currentUser)
{
    /// <summary>Determines whether the caller may write questions from the source.</summary>
    /// <param name="source">The source of the question.</param>
    public bool MayWrite(QuestionSource source) =>
        source != QuestionSource.Official || currentUser.IsInRole(UserRole.Administrator);

    /// <summary>Loads the question if the caller may change it.</summary>
    /// <param name="questionId">The identifier of the question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Question>> LoadForChangeAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await questionRepository.GetByIdAsync(questionId, cancellationToken);
        if (question is null)
            return Result.Failure<Question>(ApplicationErrors.NotFound<Question>());

        return MayWrite(question.Source) ? question : Result.Failure<Question>(ApplicationErrors.Question.OfficialAdministratorOnly);
    }
}
