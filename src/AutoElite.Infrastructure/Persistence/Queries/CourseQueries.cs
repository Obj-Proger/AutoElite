using AutoElite.Application.Courses;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads the course catalog without tracking.</summary>
internal sealed class CourseQueries(AutoEliteDbContext dbContext) : ICourseQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CourseListItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default)
    {
        var courses = dbContext.Set<Course>().AsNoTracking();
        if (category is { } wanted)
            courses = courses.Where(course => course.Category == wanted);

        if (isActive is { } active)
            courses = courses.Where(course => course.IsActive == active);

        // Categories are stored by name, so SQL would sort them alphabetically; they are sorted by their order in
        // memory instead, keeping the order of names from the database within a category.
        var rows = await courses
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Id,
                course.Name,
                course.Category,
                course.Price.Amount,
                course.Price.Currency,
                course.TheoryHours,
                course.PracticeHours,
                TopicCount = course.Topics.Count,
                course.IsActive
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.OrderBy(row => row.Category).Select(row => new CourseListItem(
                row.Id, row.Name, row.Category, row.Amount, row.Currency.Code, row.TheoryHours, row.PracticeHours,
                row.TopicCount, row.IsActive))
        ];
    }

    /// <inheritdoc />
    public async Task<CourseDetails?> GetAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await dbContext.Set<Course>().AsNoTracking()
            .Include(found => found.Topics.OrderBy(topic => topic.Position))
            .FirstOrDefaultAsync(found => found.Id == courseId, cancellationToken);

        return course is null
            ? null
            : new CourseDetails(
                course.Id, course.Name, course.Category, course.Price.Amount, course.Price.Currency.Code, course.TheoryHours,
                course.PracticeHours, course.Description, course.IsActive,
                [.. course.Topics.Select(topic => new CourseTopicItem(topic.Id, topic.Position, topic.Title, topic.Description))]);
    }
}
