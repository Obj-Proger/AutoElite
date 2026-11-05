namespace AutoElite.Application.Questions.UpdateQuestion;

/// <summary>Applies the content and checks the topic and that the external code is free among the other questions.</summary>
internal sealed class UpdateQuestionCommandHandler(QuestionAccess questionAccess, QuestionContent questionContent)
    : ICommandHandler<UpdateQuestionCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateQuestionCommand command, CancellationToken cancellationToken)
    {
        var questionResult = await questionAccess.LoadForChangeAsync(command.QuestionId, cancellationToken);
        if (questionResult.IsFailure)
            return questionResult;

        var question = questionResult.Value;
        var updateResult = question.UpdateContent(
            command.Text, command.Explanation, command.TopicId, command.Categories ?? [], command.ExternalCode,
            QuestionContent.ToDrafts(command.Options));
        if (updateResult.IsFailure)
            return updateResult;

        var topicResult = await questionContent.CheckTopicAsync(question.TopicId, cancellationToken);
        if (topicResult.IsFailure)
            return topicResult;

        return await questionContent.CheckExternalCodeAsync(question, isNew: false, cancellationToken);
    }
}
