namespace AutoElite.Application.DrivingSlots.BookDrivingSlot;

/// <summary>
/// Books an available slot for a contract of the signed-in student (FR-6.4). The vehicle must match the licence
/// category and the gearbox of the contract.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="ContractId">An active contract of the student.</param>
[RequireRoles(UserRole.Student)]
public sealed record BookDrivingSlotCommand(Guid SlotId, Guid ContractId) : ICommand;
