using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TheoryLessons;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded groups and cards and records the schedule arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeTheoryLessonQueries : ITheoryLessonQueries
{
    private readonly Dictionary<Guid, GroupTheoryLessons> _groups = [];
    private readonly Dictionary<Guid, TheoryLessonDetails> _details = [];

    public List<StudentTheoryLessonItem> StudentLessons { get; } = [];

    public (TheoryScheduleFilter Filter, DataScope Scope)? LastScheduleCall { get; private set; }

    public (Guid StudentId, TimeInterval Period)? LastStudentCall { get; private set; }

    public void Seed(Guid groupId, GroupTheoryLessons lessons) => _groups.Add(groupId, lessons);

    public void Seed(TheoryLessonDetails details) => _details.Add(details.Id, details);

    public Task<IReadOnlyList<TheoryLessonItem>> GetScheduleAsync(
        TheoryScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastScheduleCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<TheoryLessonItem>>([]);
    }

    public Task<IReadOnlyList<StudentTheoryLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default)
    {
        LastStudentCall = (studentId, period);

        return Task.FromResult<IReadOnlyList<StudentTheoryLessonItem>>([.. StudentLessons]);
    }

    public Task<GroupTheoryLessons?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_groups.GetValueOrDefault(groupId));

    public Task<TheoryLessonDetails?> GetDetailsAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(lessonId));
}
