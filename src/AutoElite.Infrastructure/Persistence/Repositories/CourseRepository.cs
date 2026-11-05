using AutoElite.Application.Courses;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads courses with their topics in curriculum order, which the rules of renumbering rely on.</summary>
internal sealed class CourseRepository(AutoEliteDbContext dbContext) : Repository<Course>(dbContext), ICourseRepository
{
    /// <inheritdoc />
    public override Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithTopics().FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Course?> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default) =>
        WithTopics().FirstOrDefaultAsync(course => course.Topics.Any(topic => topic.Id == topicId), cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Each record that refers to topics is checked here once its table is mapped: theory lessons so far; questions and
    /// the blocks of test templates follow with their modules.
    /// </remarks>
    public Task<bool> IsTopicInUseAsync(Guid topicId, CancellationToken cancellationToken = default) =>
        DbContext.Set<TheoryLesson>().AnyAsync(lesson => lesson.TopicId == topicId, cancellationToken);

    private IQueryable<Course> WithTopics() => Aggregates.Include(course => course.Topics.OrderBy(topic => topic.Position));
}
