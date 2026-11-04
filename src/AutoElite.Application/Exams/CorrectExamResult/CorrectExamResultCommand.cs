namespace AutoElite.Application.Exams.CorrectExamResult;

/// <summary>
/// Corrects a recorded exam result, for example after an appeal. The driving instructors who see the branch of the
/// group correct practice results; the responsible instructor of the group corrects theory results.
/// </summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="ContractId">The contract whose result is corrected.</param>
/// <param name="Passed">Whether the student passed.</param>
/// <param name="Score">A score from 0 to 1000; for a theory exam, the correct answers.</param>
/// <param name="Comment">The reason of the correction or another comment.</param>
[RequireRoles(UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record CorrectExamResultCommand(
    Guid ExamId,
    Guid ContractId,
    bool Passed,
    int? Score = null,
    string? Comment = null) : ICommand;
