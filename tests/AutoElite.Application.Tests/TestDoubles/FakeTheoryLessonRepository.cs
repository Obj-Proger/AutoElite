using AutoElite.Application.TheoryLessons;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeTheoryLessonRepository : InMemoryRepository<TheoryLesson>, ITheoryLessonRepository
{
    public Task<bool> HasOverlapAsync(
        Guid groupId, Guid instructorId, TimeInterval time, Guid? exceptLessonId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(lesson =>
            lesson.Id != exceptLessonId
            && lesson.Status != TheoryLessonStatus.Cancelled
            && (lesson.GroupId == groupId || lesson.InstructorId == instructorId)
            && lesson.Time.Overlaps(time)));

    public Task<IReadOnlyList<TheoryLesson>> GetScheduledStartingBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TheoryLesson>>([
            .. Items.Where(lesson =>
                lesson.Status == TheoryLessonStatus.Scheduled && lesson.Time.StartUtc > afterUtc && lesson.Time.StartUtc <= untilUtc)
        ]);
}
