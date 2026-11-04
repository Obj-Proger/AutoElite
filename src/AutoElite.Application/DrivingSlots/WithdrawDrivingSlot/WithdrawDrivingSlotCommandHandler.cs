namespace AutoElite.Application.DrivingSlots.WithdrawDrivingSlot;

/// <summary>Withdraws the caller's slot.</summary>
internal sealed class WithdrawDrivingSlotCommandHandler(
    DrivingSlotAccess slotAccess,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<WithdrawDrivingSlotCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(WithdrawDrivingSlotCommand command, CancellationToken cancellationToken)
    {
        var slotResult = await slotAccess.LoadOwnAsync(command.SlotId, cancellationToken);
        if (slotResult.IsFailure)
            return slotResult;

        return slotResult.Value.Withdraw(currentUser.GetRequiredUserId(), command.Reason, timeProvider.GetUtcNow().UtcDateTime);
    }
}
