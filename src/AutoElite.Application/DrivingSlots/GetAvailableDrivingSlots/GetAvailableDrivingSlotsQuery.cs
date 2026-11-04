namespace AutoElite.Application.DrivingSlots.GetAvailableDrivingSlots;

/// <summary>
/// Reads the slots that a contract of the signed-in student can book, the earliest first (FR-6.4): available slots
/// that have not started, for the category and the gearbox of the contract, with vehicles of the contract's branch or
/// shared ones.
/// </summary>
/// <param name="ContractId">A contract of the student.</param>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
/// <param name="InstructorId">Only slots of this instructor.</param>
[RequireRoles(UserRole.Student)]
public sealed record GetAvailableDrivingSlotsQuery(
    Guid ContractId,
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? InstructorId = null) : IQuery<IReadOnlyList<DrivingSlotItem>>;
