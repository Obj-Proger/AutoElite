namespace AutoElite.Application.Exams.StartExamTest;

/// <summary>
/// Starts the attempt of the signed-in student at a theory exam of their group, once the exam has begun. The attempt
/// follows the template of the exam, and its result becomes the exam result when it finishes. Each contract takes
/// the exam once.
/// </summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="ContractId">The contract of the student, a current member of the group of the exam.</param>
[RequireRoles(UserRole.Student)]
public sealed record StartExamTestCommand(Guid ExamId, Guid ContractId) : ICommand<Guid>;
