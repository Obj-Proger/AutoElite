using AutoElite.Application.Contracts;

namespace AutoElite.Application.Exams.RecordPracticeExamResult;

/// <summary>Loads the exam and the contract and records the result.</summary>
internal sealed class RecordPracticeExamResultCommandHandler(
    ExamAccess examAccess,
    IContractRepository contractRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<RecordPracticeExamResultCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RecordPracticeExamResultCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await examAccess.LoadForResultsAsync(command.ExamId, cancellationToken);
        if (loadResult.IsFailure)
            return loadResult;

        var contract = await contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
            return Result.Failure(ApplicationErrors.NotFound<Contract>());

        var (exam, group) = loadResult.Value;
        var recordResult = exam.RecordPracticeResult(
            group, contract, command.Passed, command.Score, command.Comment, currentUser.GetRequiredUserId(),
            timeProvider.GetUtcNow().UtcDateTime);

        return recordResult.IsFailure ? Result.Failure(recordResult.Error) : Result.Success();
    }
}
