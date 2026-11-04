namespace AutoElite.Application.DrivingSlots.OfferDrivingSlot;

/// <summary>
/// Offers a practice slot of the signed-in instructor that students can book (FR-6.3). The slot takes the licence
/// category and the gearbox of the vehicle, so only students whose contract matches can book it.
/// </summary>
/// <param name="VehicleId">A vehicle in service of the instructor's branch or a shared one.</param>
/// <param name="StartUtc">The start, in UTC and in the future.</param>
/// <param name="EndUtc">The end, in UTC; the slot lasts at most 4 hours.</param>
/// <param name="TrainingGroundId">The ground where the lesson starts; empty for none.</param>
/// <param name="RouteId">The route of the lesson; empty for none.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record OfferDrivingSlotCommand(
    Guid VehicleId,
    DateTime StartUtc,
    DateTime EndUtc,
    Guid? TrainingGroundId = null,
    Guid? RouteId = null) : ICommand<Guid>;
