namespace AutoElite.Application.Exams.ScheduleExam;

/// <summary>
/// Schedules an internal exam for a study group (FR-8.1); the students of the group are notified. The responsible
/// theory instructor of the group schedules both kinds. A theory exam is taken as a test from an active template for
/// the category of the group; a practice exam has no template.
/// </summary>
/// <param name="GroupId">The study group.</param>
/// <param name="Kind">Theory or practice.</param>
/// <param name="ScheduledAtUtc">When the exam takes place, in UTC and in the future.</param>
/// <param name="TestTemplateId">The template of a theory exam; empty for a practice exam.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record ScheduleExamCommand(Guid GroupId, ExamKind Kind, DateTime ScheduledAtUtc, Guid? TestTemplateId = null)
    : ICommand<Guid>;
