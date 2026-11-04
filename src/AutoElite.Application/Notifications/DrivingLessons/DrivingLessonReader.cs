using AutoElite.Application.Contracts;
using AutoElite.Application.DrivingSlots;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>Loads what the notifications about a driving booking need.</summary>
internal sealed class DrivingLessonReader(
    IDrivingSlotRepository slotRepository,
    IContractRepository contractRepository,
    LocalTimes localTimes)
{
    /// <summary>Loads the slot, the booking and the local start of the lesson.</summary>
    /// <param name="slotId">The identifier of the slot.</param>
    /// <param name="bookingId">The identifier of the booking.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The details, or <see langword="null"/> if the slot or the booking no longer exists.</returns>
    public async Task<DrivingLessonDetails?> LoadAsync(Guid slotId, Guid bookingId, CancellationToken cancellationToken)
    {
        var slot = await slotRepository.GetByIdAsync(slotId, cancellationToken);
        var booking = slot?.Bookings.SingleOrDefault(item => item.Id == bookingId);
        if (slot is null || booking is null)
            return null;

        var contract = await contractRepository.GetByIdAsync(booking.ContractId, cancellationToken);
        var localStart = await localTimes.ToLocalAsync(slot.Time.StartUtc, contract?.BranchId, cancellationToken);

        return new DrivingLessonDetails(slot, booking, contract?.BranchId, localStart);
    }
}
