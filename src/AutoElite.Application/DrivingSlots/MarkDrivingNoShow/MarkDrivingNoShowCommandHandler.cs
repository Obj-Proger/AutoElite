using AutoElite.Application.Contracts;

namespace AutoElite.Application.DrivingSlots.MarkDrivingNoShow;

/// <summary>Records the no-show on the caller's slot.</summary>
internal sealed class MarkDrivingNoShowCommandHandler(
    DrivingSlotAccess slotAccess,
    IContractRepository contractRepository,
    TimeProvider timeProvider) : ICommandHandler<MarkDrivingNoShowCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkDrivingNoShowCommand command, CancellationToken cancellationToken)
    {
        var slotResult = await slotAccess.LoadOwnAsync(command.SlotId, cancellationToken);
        if (slotResult.IsFailure)
            return slotResult;

        var slot = slotResult.Value;
        var contractResult = await BookedContract.LoadAsync(slot, contractRepository, cancellationToken);
        if (contractResult.IsFailure)
            return contractResult;

        return slot.MarkNoShow(contractResult.Value, command.Note, timeProvider.GetUtcNow().UtcDateTime);
    }
}
