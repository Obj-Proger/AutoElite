namespace AutoElite.Application.Exams.RescheduleExam;

/// <summary>Moves a scheduled exam that has no results yet; the students of the group are notified.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="ScheduledAtUtc">The new time, in UTC and in the future.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record RescheduleExamCommand(Guid ExamId, DateTime ScheduledAtUtc) : ICommand;
