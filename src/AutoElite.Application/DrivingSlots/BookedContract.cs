using AutoElite.Application.Contracts;

namespace AutoElite.Application.DrivingSlots;

/// <summary>Loads the contract of the active booking of a slot, which the lesson outcome is recorded on.</summary>
internal static class BookedContract
{
    /// <summary>Loads the contract of the booking.</summary>
    /// <param name="slot">The slot.</param>
    /// <param name="contractRepository">The contract repository.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The contract, or a failure if the slot is not booked or the contract is missing.</returns>
    public static async Task<Result<Contract>> LoadAsync(
        DrivingSlot slot, IContractRepository contractRepository, CancellationToken cancellationToken)
    {
        if (slot.Status != DrivingSlotStatus.Booked || slot.CurrentBooking is not { } booking)
            return Result.Failure<Contract>(DomainErrors.DrivingSlot.NotBooked);

        var contract = await contractRepository.GetByIdAsync(booking.ContractId, cancellationToken);

        return contract is null ? Result.Failure<Contract>(ApplicationErrors.NotFound<Contract>()) : contract;
    }
}
