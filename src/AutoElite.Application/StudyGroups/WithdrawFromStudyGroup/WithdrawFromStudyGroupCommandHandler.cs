namespace AutoElite.Application.StudyGroups.WithdrawFromStudyGroup;

/// <summary>Withdraws the student.</summary>
internal sealed class WithdrawFromStudyGroupCommandHandler(StudyGroupAccess groupAccess, TimeProvider timeProvider)
    : ICommandHandler<WithdrawFromStudyGroupCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(WithdrawFromStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = await groupAccess.LoadAsync(command.GroupId, cancellationToken);

        return groupResult.IsFailure
            ? Result.Failure(groupResult.Error)
            : groupResult.Value.Withdraw(command.ContractId, timeProvider.GetUtcNow().UtcDateTime);
    }
}
