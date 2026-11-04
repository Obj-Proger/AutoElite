namespace AutoElite.Application.DrivingSlots.CompleteDrivingLesson;

/// <summary>
/// Completes a booked lesson of the signed-in instructor that has started: records the actual hours, the skill
/// grades and a note (FR-6.6). The hours are added to the practice of the contract and the grades update its quality
/// score (FR-6.7).
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="ActualHours">How long the lesson actually lasted, in hours, with at most two decimals.</param>
/// <param name="Grades">The grades, at most one per skill; empty for an ungraded lesson.</param>
/// <param name="Note">The instructor's note about the lesson, if any.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record CompleteDrivingLessonCommand(
    Guid SlotId,
    decimal ActualHours,
    IReadOnlyList<SkillGradeInput>? Grades = null,
    string? Note = null) : ICommand;
