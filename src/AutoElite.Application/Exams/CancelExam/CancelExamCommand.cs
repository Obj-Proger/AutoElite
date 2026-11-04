namespace AutoElite.Application.Exams.CancelExam;

/// <summary>Cancels a scheduled exam that has no results yet, with a reason; the students of the group are notified.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="Reason">Why the exam is cancelled. Required.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record CancelExamCommand(Guid ExamId, string? Reason) : ICommand;
