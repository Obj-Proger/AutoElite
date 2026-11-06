using AutoElite.Application.Courses;
using AutoElite.Infrastructure.Persistence.Conversions;

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
    /// A topic is in use when a question, a theory lesson or a block of a test template refers to it. The blocks are a
    /// JSON column, so that check is a query over the JSON.
    /// </remarks>
    public async Task<bool> IsTopicInUseAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        if (await DbContext.Set<Question>().AnyAsync(question => question.TopicId == topicId, cancellationToken)
            || await DbContext.Set<TheoryLesson>().AnyAsync(lesson => lesson.TopicId == topicId, cancellationToken))
            return true;

        var topic = topicId.ToString();
        var inTemplates = await DbContext.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1
                    FROM test_templates AS template, jsonb_array_elements(template.blocks) AS block
                    WHERE jsonb_exists(block -> {TestBlocksConverter.TopicIdsProperty}, {topic})
                ) AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return inTemplates.Single();
    }

    private IQueryable<Course> WithTopics() => Aggregates.Include(course => course.Topics.OrderBy(topic => topic.Position));
}
