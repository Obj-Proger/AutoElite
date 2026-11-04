namespace AutoElite.Application.DrivingSlots.GetDrivingSchedule;

/// <summary>
/// Reads the driving slots that overlap a period with the bookings that hold them, the earliest first. Managers see the
/// slots with vehicles of their branch and with shared vehicles; a driving instructor sees only their own slots.
/// </summary>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
/// <param name="InstructorId">Only slots of this instructor.</param>
/// <param name="Status">Only slots in this status.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor)]
public sealed record GetDrivingScheduleQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? InstructorId = null,
    DrivingSlotStatus? Status = null) : IQuery<IReadOnlyList<DrivingScheduleItem>>;
