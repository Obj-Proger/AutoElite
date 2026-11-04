namespace AutoElite.Application.Questions.ReactivateQuestion;

/// <summary>Reactivates the question.</summary>
internal sealed class ReactivateQuestionCommandHandler(QuestionAccess questionAccess) : ICommandHandler<ReactivateQuestionCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateQuestionCommand command, CancellationToken cancellationToken)
    {
        var questionResult = await questionAccess.LoadForChangeAsync(command.QuestionId, cancellationToken);

        return questionResult.IsFailure ? questionResult : questionResult.Value.Reactivate();
    }
}
