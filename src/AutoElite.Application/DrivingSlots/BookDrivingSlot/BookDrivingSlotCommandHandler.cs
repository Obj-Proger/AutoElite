using AutoElite.Application.Contracts;

namespace AutoElite.Application.DrivingSlots.BookDrivingSlot;

/// <summary>
/// Checks that the contract is the caller's own or, for a manager or an administrator, inside their branch scope, and
/// that its student is free at that time, and books the slot.
/// </summary>
internal sealed class BookDrivingSlotCommandHandler(
    IDrivingSlotRepository slotRepository,
    IContractRepository contractRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<BookDrivingSlotCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(BookDrivingSlotCommand command, CancellationToken cancellationToken)
    {
        var callerId = currentUser.GetRequiredUserId();
        var contract = await contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
            return Result.Failure(ApplicationErrors.NotFound<Contract>());

        // A contract of another student is not found for a student, as one outside the scope is for the staff.
        var isOwn = contract.StudentId == callerId;
        var isStaff = currentUser.IsInRole(UserRole.Manager) || currentUser.IsInRole(UserRole.Administrator);
        if (!isOwn && !(isStaff && DataScope.For(currentUser).Includes(contract.BranchId)))
            return Result.Failure(ApplicationErrors.NotFound<Contract>());

        var slot = await slotRepository.GetByIdAsync(command.SlotId, cancellationToken);
        if (slot is null)
            return Result.Failure(ApplicationErrors.NotFound<DrivingSlot>());

        var bookingResult = slot.Book(contract, timeProvider.GetUtcNow().UtcDateTime, isOwn ? null : callerId);
        if (bookingResult.IsFailure)
            return bookingResult;

        // Checked after the domain rules, so that a slot that cannot be booked anyway reports why.
        return await slotRepository.StudentHasBookingAtAsync(contract.StudentId, slot.Time, slot.Id, cancellationToken)
            ? Result.Failure(ApplicationErrors.DrivingSlot.StudentBusy)
            : Result.Success();
    }
}
