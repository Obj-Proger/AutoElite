namespace AutoElite.Application.DrivingSlots.BookDrivingSlot;

/// <summary>
/// Books an available slot for a contract (FR-6.4): a student books for a contract of their own, a manager or an
/// administrator for a contract of a student in their branch scope, such as a student who calls the school. The
/// vehicle must match the licence category and the gearbox of the contract.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="ContractId">An active contract of the student.</param>
[RequireRoles(UserRole.Student, UserRole.Manager, UserRole.Administrator)]
public sealed record BookDrivingSlotCommand(Guid SlotId, Guid ContractId) : ICommand;
