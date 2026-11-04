namespace AutoElite.Application.DrivingSlots.MarkDrivingNoShow;

/// <summary>
/// Records that the student did not come to a booked lesson of the signed-in instructor that has started. No hours are
/// counted.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="Note">The instructor's note, if any.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record MarkDrivingNoShowCommand(Guid SlotId, string? Note = null) : ICommand;
