namespace AutoElite.Application.Questions.CreateQuestion;

/// <summary>Checks the source, the topic and the external code, and adds the question.</summary>
internal sealed class CreateQuestionCommandHandler(
    IQuestionRepository questionRepository,
    QuestionAccess questionAccess,
    QuestionContent questionContent) : ICommandHandler<CreateQuestionCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        if (!questionAccess.MayWrite(command.Source))
            return Result.Failure<Guid>(ApplicationErrors.Question.OfficialAdministratorOnly);

        var questionResult = Question.Create(
            command.Text, command.Explanation, null, command.TopicId, command.Categories ?? [], command.Source, command.ExternalCode,
            QuestionContent.ToDrafts(command.Options));
        if (questionResult.IsFailure)
            return Result.Failure<Guid>(questionResult.Error);

        var question = questionResult.Value;
        var topicResult = await questionContent.CheckTopicAsync(question.TopicId, cancellationToken);
        if (topicResult.IsFailure)
            return Result.Failure<Guid>(topicResult.Error);

        var codeResult = await questionContent.CheckExternalCodeAsync(question, isNew: true, cancellationToken);
        if (codeResult.IsFailure)
            return Result.Failure<Guid>(codeResult.Error);

        questionRepository.Add(question);

        return question.Id;
    }
}
