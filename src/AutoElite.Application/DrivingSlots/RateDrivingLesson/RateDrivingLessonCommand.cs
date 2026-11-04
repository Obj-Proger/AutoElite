namespace AutoElite.Application.DrivingSlots.RateDrivingLesson;

/// <summary>
/// Rates a completed lesson of the signed-in student, once (FR-6.8). A rating at or below the school's threshold
/// notifies the responsible managers.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="Rating">The rating, from 1 to 5.</param>
/// <param name="Comment">A comment, if any.</param>
[RequireRoles(UserRole.Student)]
public sealed record RateDrivingLessonCommand(Guid SlotId, int Rating, string? Comment = null) : ICommand;
