namespace AutoElite.Application.DrivingSlots.WithdrawDrivingSlot;

/// <summary>
/// Withdraws an available or booked slot of the signed-in instructor, with a reason. A booking of the slot is
/// cancelled and the student is notified.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="Reason">Why the slot is withdrawn. Required.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record WithdrawDrivingSlotCommand(Guid SlotId, string? Reason) : ICommand;
