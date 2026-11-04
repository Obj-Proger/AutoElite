using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Exams;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded groups and cards and records the schedule arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeExamQueries : IExamQueries
{
    private readonly Dictionary<Guid, GroupExams> _groups = [];
    private readonly Dictionary<Guid, ExamDetails> _details = [];

    public List<StudentExamItem> StudentExams { get; } = [];

    public (ExamScheduleFilter Filter, DataScope Scope)? LastScheduleCall { get; private set; }

    public Guid? LastStudentId { get; private set; }

    public void Seed(Guid groupId, GroupExams exams) => _groups.Add(groupId, exams);

    public void Seed(ExamDetails details) => _details.Add(details.Exam.Id, details);

    public Task<IReadOnlyList<ExamItem>> GetScheduleAsync(
        ExamScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastScheduleCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<ExamItem>>([]);
    }

    public Task<GroupExams?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_groups.GetValueOrDefault(groupId));

    public Task<IReadOnlyList<StudentExamItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        LastStudentId = studentId;

        return Task.FromResult<IReadOnlyList<StudentExamItem>>([.. StudentExams]);
    }

    public Task<ExamDetails?> GetDetailsAsync(Guid examId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(examId));
}
