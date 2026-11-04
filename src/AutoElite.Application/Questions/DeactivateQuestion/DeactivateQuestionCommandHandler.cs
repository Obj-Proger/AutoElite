namespace AutoElite.Application.Questions.DeactivateQuestion;

/// <summary>Deactivates the question.</summary>
internal sealed class DeactivateQuestionCommandHandler(QuestionAccess questionAccess) : ICommandHandler<DeactivateQuestionCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateQuestionCommand command, CancellationToken cancellationToken)
    {
        var questionResult = await questionAccess.LoadForChangeAsync(command.QuestionId, cancellationToken);

        return questionResult.IsFailure ? questionResult : questionResult.Value.Deactivate();
    }
}
