using AutoElite.Application.Contracts;

namespace AutoElite.Application.DrivingSlots.GetAvailableDrivingSlots;

/// <summary>Checks that the contract is the caller's and reads the slots that match it.</summary>
internal sealed class GetAvailableDrivingSlotsQueryHandler(
    IDrivingSlotQueries slotQueries,
    IContractQueries contractQueries,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<GetAvailableDrivingSlotsQuery, IReadOnlyList<DrivingSlotItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DrivingSlotItem>>> Handle(
        GetAvailableDrivingSlotsQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<DrivingSlotItem>>(periodResult.Error);

        var contract = await contractQueries.GetDetailsAsync(query.ContractId, cancellationToken);
        if (contract is null || contract.StudentId != currentUser.GetRequiredUserId())
            return Result.Failure<IReadOnlyList<DrivingSlotItem>>(ApplicationErrors.NotFound<Contract>());

        // Only an active contract can book, so there is nothing to offer to a closed one.
        if (contract.Status != ContractStatus.Active)
            return Result.Success<IReadOnlyList<DrivingSlotItem>>([]);

        var filter = new AvailableDrivingSlotFilter(
            periodResult.Value, timeProvider.GetUtcNow().UtcDateTime, contract.Category, contract.Transmission, query.InstructorId);
        var scope = contract.BranchId is { } branchId ? DataScope.ForBranch(branchId) : DataScope.SchoolWide;

        return Result.Success(await slotQueries.GetAvailableAsync(filter, scope, cancellationToken));
    }
}
