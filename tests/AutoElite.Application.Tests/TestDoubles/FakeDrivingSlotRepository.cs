using AutoElite.Application.DrivingSlots;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeDrivingSlotRepository : InMemoryRepository<DrivingSlot>, IDrivingSlotRepository
{
    public Task<bool> HasOverlapAsync(
        Guid instructorId, Guid vehicleId, TimeInterval time, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(slot =>
            slot.Status != DrivingSlotStatus.Withdrawn
            && (slot.InstructorId == instructorId || slot.VehicleId == vehicleId)
            && slot.Time.Overlaps(time)));

    public Task<bool> StudentHasBookingAtAsync(
        Guid studentId, TimeInterval time, Guid exceptSlotId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(slot =>
            slot.Id != exceptSlotId
            && slot.Status != DrivingSlotStatus.Withdrawn
            && slot.CurrentBooking?.StudentId == studentId
            && slot.Time.Overlaps(time)));

    public Task<IReadOnlyList<DrivingSlot>> GetBookedForContractAfterAsync(
        Guid contractId, DateTime afterUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DrivingSlot>>([
            .. Items.Where(slot =>
                slot.Status == DrivingSlotStatus.Booked && slot.CurrentBooking!.ContractId == contractId && slot.Time.StartUtc > afterUtc)
        ]);
}
