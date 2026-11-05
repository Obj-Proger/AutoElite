using AutoElite.Application.TheoryLessons;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads theory lessons with their attendance marks.</summary>
internal sealed class TheoryLessonRepository(AutoEliteDbContext dbContext)
    : Repository<TheoryLesson>(dbContext), ITheoryLessonRepository
{
    /// <inheritdoc />
    public override Task<TheoryLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Aggregates.Include(lesson => lesson.Attendance).FirstOrDefaultAsync(lesson => lesson.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasOverlapAsync(
        Guid groupId, Guid instructorId, TimeInterval time, Guid? exceptLessonId = null,
        CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(
            lesson => lesson.Id != exceptLessonId
                && lesson.Status != TheoryLessonStatus.Cancelled
                && (lesson.GroupId == groupId || lesson.InstructorId == instructorId)
                && lesson.Time.StartUtc < time.EndUtc
                && time.StartUtc < lesson.Time.EndUtc,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TheoryLesson>> GetScheduledStartingBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        await Aggregates
            .Include(lesson => lesson.Attendance)
            .Where(lesson => lesson.Status == TheoryLessonStatus.Scheduled
                && lesson.Time.StartUtc > afterUtc
                && lesson.Time.StartUtc <= untilUtc)
            .ToListAsync(cancellationToken);
}
