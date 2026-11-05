namespace AutoElite.Application.Questions.RemoveQuestionImage;

/// <summary>Clears the illustration if the caller may change the question.</summary>
internal sealed class RemoveQuestionImageCommandHandler(QuestionAccess questionAccess) : ICommandHandler<RemoveQuestionImageCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveQuestionImageCommand command, CancellationToken cancellationToken)
    {
        var questionResult = await questionAccess.LoadForChangeAsync(command.QuestionId, cancellationToken);

        return questionResult.IsFailure ? questionResult : questionResult.Value.RemoveImage();
    }
}
