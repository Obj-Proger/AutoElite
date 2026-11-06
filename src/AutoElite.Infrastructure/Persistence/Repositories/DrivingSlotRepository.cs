using AutoElite.Application.DrivingSlots;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads driving slots with their bookings, which the booking rules need.</summary>
internal sealed class DrivingSlotRepository(AutoEliteDbContext dbContext)
    : Repository<DrivingSlot>(dbContext), IDrivingSlotRepository
{
    /// <inheritdoc />
    public override Task<DrivingSlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithBookings().FirstOrDefaultAsync(slot => slot.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasOverlapAsync(
        Guid instructorId, Guid vehicleId, TimeInterval time, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(
            slot => slot.Status != DrivingSlotStatus.Withdrawn
                && (slot.InstructorId == instructorId || slot.VehicleId == vehicleId)
                && slot.Time.StartUtc < time.EndUtc
                && time.StartUtc < slot.Time.EndUtc,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> StudentHasBookingAtAsync(
        Guid studentId, TimeInterval time, Guid exceptSlotId, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(
            slot => slot.Id != exceptSlotId
                && slot.Status != DrivingSlotStatus.Withdrawn
                && slot.Time.StartUtc < time.EndUtc
                && time.StartUtc < slot.Time.EndUtc
                && slot.Bookings.Any(booking =>
                    booking.StudentId == studentId && booking.Status != DrivingBookingStatus.Cancelled),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DrivingSlot>> GetBookedForContractAfterAsync(
        Guid contractId, DateTime afterUtc, CancellationToken cancellationToken = default) =>
        await WithBookings()
            .Where(slot => slot.Status == DrivingSlotStatus.Booked
                && slot.Time.StartUtc > afterUtc
                && slot.Bookings.Any(booking =>
                    booking.ContractId == contractId && booking.Status != DrivingBookingStatus.Cancelled))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DrivingSlot>> GetBookedStartingBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        await WithBookings()
            .Where(slot => slot.Status == DrivingSlotStatus.Booked
                && slot.Time.StartUtc > afterUtc
                && slot.Time.StartUtc <= untilUtc)
            .ToListAsync(cancellationToken);

    private IQueryable<DrivingSlot> WithBookings() => Aggregates.Include(slot => slot.Bookings);
}
