namespace AutoElite.Application.Exams;

/// <summary>The conditions of the exam schedule; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Period">Only exams scheduled within this time.</param>
/// <param name="Kind">Only theory or only practice exams.</param>
/// <param name="GroupId">Only exams of this group.</param>
/// <param name="ResponsibleInstructorId">Only exams of the groups of this theory instructor.</param>
public sealed record ExamScheduleFilter(TimeInterval Period, ExamKind? Kind, Guid? GroupId, Guid? ResponsibleInstructorId);

/// <summary>An exam in a list.</summary>
/// <param name="Id">The identifier of the exam.</param>
/// <param name="GroupId">The study group.</param>
/// <param name="GroupName">The name of the group.</param>
/// <param name="BranchId">The branch of the group, if any.</param>
/// <param name="ResponsibleInstructorId">The responsible theory instructor of the group.</param>
/// <param name="Kind">Theory or practice.</param>
/// <param name="TestTemplateId">The template of a theory exam.</param>
/// <param name="TestTemplateName">The name of the template, if any.</param>
/// <param name="ScheduledAtUtc">When the exam takes place.</param>
/// <param name="Status">The exam status.</param>
/// <param name="CancellationReason">Why the exam was cancelled, if it was.</param>
/// <param name="ResultCount">The number of recorded results.</param>
public sealed record ExamItem(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid? BranchId,
    Guid ResponsibleInstructorId,
    ExamKind Kind,
    Guid? TestTemplateId,
    string? TestTemplateName,
    DateTime ScheduledAtUtc,
    ExamStatus Status,
    string? CancellationReason,
    int ResultCount);

/// <summary>The result of a student at an exam.</summary>
/// <param name="ContractId">The contract.</param>
/// <param name="ContractNumber">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="Passed">Whether the student passed.</param>
/// <param name="Score">The score; for a theory exam, the correct answers.</param>
/// <param name="Comment">A comment, if any.</param>
/// <param name="TestSessionId">The attempt of a theory exam.</param>
/// <param name="RecordedByUserId">Who recorded a practice result; <see langword="null"/> for a theory result.</param>
/// <param name="RecordedAtUtc">When the result was recorded.</param>
/// <param name="CorrectedByUserId">Who last corrected the result, if anyone.</param>
/// <param name="CorrectedAtUtc">When the result was last corrected, if it was.</param>
public sealed record ExamResultItem(
    Guid ContractId,
    string ContractNumber,
    Guid StudentId,
    string StudentName,
    bool Passed,
    int? Score,
    string? Comment,
    Guid? TestSessionId,
    Guid? RecordedByUserId,
    DateTime RecordedAtUtc,
    Guid? CorrectedByUserId,
    DateTime? CorrectedAtUtc);

/// <summary>The exams of a group with what is needed to decide who may read them.</summary>
/// <param name="BranchId">The branch of the group, if any.</param>
/// <param name="ResponsibleInstructorId">The responsible theory instructor of the group.</param>
/// <param name="Exams">All exams of the group, cancelled ones included, the earliest first.</param>
public sealed record GroupExams(Guid? BranchId, Guid ResponsibleInstructorId, IReadOnlyList<ExamItem> Exams);

/// <summary>The card of an exam with the results of the students.</summary>
/// <param name="Exam">The exam.</param>
/// <param name="Results">The results, by student name.</param>
public sealed record ExamDetails(ExamItem Exam, IReadOnlyList<ExamResultItem> Results);

/// <summary>An exam of a student with the student's own result.</summary>
/// <param name="Exam">The exam.</param>
/// <param name="Result">The student's result, or <see langword="null"/> if not recorded yet.</param>
public sealed record StudentExamItem(ExamItem Exam, ExamResultItem? Result);
