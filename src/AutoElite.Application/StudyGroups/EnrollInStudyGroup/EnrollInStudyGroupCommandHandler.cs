using AutoElite.Application.Contracts;

namespace AutoElite.Application.StudyGroups.EnrollInStudyGroup;

/// <summary>Checks that the contract and the group belong together and enrolls the student.</summary>
internal sealed class EnrollInStudyGroupCommandHandler(
    StudyGroupAccess groupAccess,
    ContractAccess contractAccess,
    TimeProvider timeProvider) : ICommandHandler<EnrollInStudyGroupCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(EnrollInStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = await groupAccess.LoadAsync(command.GroupId, cancellationToken);
        if (groupResult.IsFailure)
            return Result.Failure<Guid>(groupResult.Error);

        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure<Guid>(contractResult.Error);

        var group = groupResult.Value;
        var contract = contractResult.Value;
        if (group.BranchId is { } groupBranchId && contract.BranchId is { } contractBranchId && groupBranchId != contractBranchId)
            return Result.Failure<Guid>(ApplicationErrors.StudyGroup.ContractOfAnotherBranch);

        var enrollmentResult = group.Enroll(contract, timeProvider.GetUtcNow().UtcDateTime);

        return enrollmentResult.IsFailure ? Result.Failure<Guid>(enrollmentResult.Error) : enrollmentResult.Value.Id;
    }
}
