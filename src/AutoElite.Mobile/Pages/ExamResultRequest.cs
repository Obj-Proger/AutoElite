namespace AutoElite.Mobile.Pages;

/// <summary>A result of an exam to record or correct, passed to the page that does it.</summary>
/// <param name="ExamId">The exam.</param>
/// <param name="ContractId">The contract of the student.</param>
/// <param name="Title">The exam and the student, shown on the page.</param>
/// <param name="IsCorrection">Whether a recorded result is corrected.</param>
/// <param name="Passed">Whether the student passed, for a correction.</param>
/// <param name="Score">The score, for a correction.</param>
public sealed record ExamResultRequest(
    Guid ExamId, Guid ContractId, string Title, bool IsCorrection, bool? Passed = null, int? Score = null);
