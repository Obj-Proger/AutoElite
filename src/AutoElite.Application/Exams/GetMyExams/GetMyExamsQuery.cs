namespace AutoElite.Application.Exams.GetMyExams;

/// <summary>Reads the exams of the signed-in student with their own results, the latest first.</summary>
[RequireRoles(UserRole.Student)]
public sealed record GetMyExamsQuery : IQuery<IReadOnlyList<StudentExamItem>>;
