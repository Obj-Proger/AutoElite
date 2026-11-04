using AutoElite.Application.Contracts;
using AutoElite.Application.Settings;

namespace AutoElite.Application.DrivingSlots.CancelDrivingBooking;

/// <summary>Checks that the caller may cancel the booking and cancels it under the school's cancellation notice.</summary>
internal sealed class CancelDrivingBookingCommandHandler(
    IDrivingSlotRepository slotRepository,
    IContractRepository contractRepository,
    SchoolSettingsProvider settingsProvider,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<CancelDrivingBookingCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CancelDrivingBookingCommand command, CancellationToken cancellationToken)
    {
        var slot = await slotRepository.GetByIdAsync(command.SlotId, cancellationToken);
        if (slot is null || !await MayCancelAsync(slot, cancellationToken))
            return Result.Failure(ApplicationErrors.NotFound<DrivingSlot>());

        var settings = await settingsProvider.GetAsync(cancellationToken);

        return slot.CancelBooking(
            currentUser.GetRequiredUserId(), command.Reason, settings.StudentCancellationNotice, timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task<bool> MayCancelAsync(DrivingSlot slot, CancellationToken cancellationToken)
    {
        var callerId = currentUser.GetRequiredUserId();
        if (slot.InstructorId == callerId)
            return true;

        if (slot.CurrentBooking is not { } booking)
            return false;

        if (booking.StudentId == callerId)
            return true;

        if (!currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager))
            return false;

        var contract = await contractRepository.GetByIdAsync(booking.ContractId, cancellationToken);

        return contract is not null && DataScope.For(currentUser).Includes(contract.BranchId);
    }
}
