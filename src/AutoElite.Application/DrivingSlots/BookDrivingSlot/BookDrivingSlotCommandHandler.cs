using AutoElite.Application.Contracts;

namespace AutoElite.Application.DrivingSlots.BookDrivingSlot;

/// <summary>Checks that the contract is the caller's and that the caller is free at that time, and books the slot.</summary>
internal sealed class BookDrivingSlotCommandHandler(
    IDrivingSlotRepository slotRepository,
    IContractRepository contractRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<BookDrivingSlotCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(BookDrivingSlotCommand command, CancellationToken cancellationToken)
    {
        var studentId = currentUser.GetRequiredUserId();
        var contract = await contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null || contract.StudentId != studentId)
            return Result.Failure(ApplicationErrors.NotFound<Contract>());

        var slot = await slotRepository.GetByIdAsync(command.SlotId, cancellationToken);
        if (slot is null)
            return Result.Failure(ApplicationErrors.NotFound<DrivingSlot>());

        var bookingResult = slot.Book(contract, timeProvider.GetUtcNow().UtcDateTime);
        if (bookingResult.IsFailure)
            return bookingResult;

        // Checked after the domain rules, so that a slot that cannot be booked anyway reports why.
        return await slotRepository.StudentHasBookingAtAsync(studentId, slot.Time, slot.Id, cancellationToken)
            ? Result.Failure(ApplicationErrors.DrivingSlot.StudentBusy)
            : Result.Success();
    }
}
