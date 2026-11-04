namespace AutoElite.Application.DrivingSlots.GetDrivingSlot;

/// <summary>
/// Reads the card of a driving slot with all of its bookings. The instructor of the slot sees it, and so do the staff
/// who see its vehicle.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor)]
public sealed record GetDrivingSlotQuery(Guid SlotId) : IQuery<DrivingSlotDetails>;
