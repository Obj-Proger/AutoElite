using AutoElite.Application.Questions;

namespace AutoElite.Application.Practice.AnswerPracticeQuestion;

/// <summary>
/// Checks the contract, the question and the option, then creates or updates the record of the student for the
/// question. A question taken out of use or of another category is not found, as in the practice itself.
/// </summary>
internal sealed class AnswerPracticeQuestionCommandHandler(
    PracticeContracts contracts,
    IQuestionRepository questionRepository,
    IPracticeRecordRepository recordRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<AnswerPracticeQuestionCommand, PracticeAnswerResult>
{
    /// <inheritdoc />
    public async Task<Result<PracticeAnswerResult>> Handle(AnswerPracticeQuestionCommand command, CancellationToken cancellationToken)
    {
        var contract = await contracts.LoadAsync(command.ContractId, cancellationToken);
        if (contract.IsFailure)
            return Result.Failure<PracticeAnswerResult>(contract.Error);

        var question = await questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question is null || !question.IsActive || !question.Categories.Contains(contract.Value.Category))
            return Result.Failure<PracticeAnswerResult>(ApplicationErrors.NotFound<Question>());

        if (!question.Options.Any(option => option.Id == command.OptionId))
            return Result.Failure<PracticeAnswerResult>(ApplicationErrors.Practice.OptionNotInQuestion);

        var correctOptionId = question.CorrectOption.Id;
        var isCorrect = command.OptionId == correctOptionId;
        var studentId = currentUser.GetRequiredUserId();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var record = await recordRepository.FindAsync(studentId, question.Id, cancellationToken);
        if (record is null)
        {
            var started = PracticeRecord.Start(studentId, question.Id, isCorrect, utcNow);
            if (started.IsFailure)
                return Result.Failure<PracticeAnswerResult>(started.Error);

            recordRepository.Add(started.Value);
        }
        else
        {
            record.Record(isCorrect, utcNow);
        }

        return new PracticeAnswerResult(isCorrect, correctOptionId, question.Explanation);
    }
}
