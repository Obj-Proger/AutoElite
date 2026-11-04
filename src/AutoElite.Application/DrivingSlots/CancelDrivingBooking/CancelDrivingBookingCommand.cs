namespace AutoElite.Application.DrivingSlots.CancelDrivingBooking;

/// <summary>
/// Cancels the booking of a slot before the lesson starts, with a reason; the slot becomes available again (FR-6.5).
/// The student may cancel their own booking until the cancellation notice set by the school; the instructor of the
/// slot and the staff of the student's contract may cancel it until the start.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="Reason">Why the booking is cancelled. Required.</param>
[RequireRoles(UserRole.Student, UserRole.DrivingInstructor, UserRole.Manager, UserRole.Administrator)]
public sealed record CancelDrivingBookingCommand(Guid SlotId, string? Reason) : ICommand;
