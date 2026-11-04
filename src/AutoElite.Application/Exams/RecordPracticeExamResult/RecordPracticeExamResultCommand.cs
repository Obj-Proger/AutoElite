namespace AutoElite.Application.Exams.RecordPracticeExamResult;

/// <summary>
/// Records the result of a student at a practice exam that has begun. The driving instructors who see the branch of
/// the group record the results.
/// </summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="ContractId">The contract of a current member of the group.</param>
/// <param name="Passed">Whether the student passed.</param>
/// <param name="Score">A score from 0 to 1000, if the school uses one.</param>
/// <param name="Comment">A comment, if any.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record RecordPracticeExamResultCommand(
    Guid ExamId,
    Guid ContractId,
    bool Passed,
    int? Score = null,
    string? Comment = null) : ICommand;
