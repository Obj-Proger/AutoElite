namespace AutoElite.Application.Exams.GetExam;

/// <summary>Reads the card of an exam with the results of all students.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor, UserRole.DrivingInstructor)]
public sealed record GetExamQuery(Guid ExamId) : IQuery<ExamDetails>;
