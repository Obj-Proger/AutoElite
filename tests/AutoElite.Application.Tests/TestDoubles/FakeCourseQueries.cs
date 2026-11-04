using AutoElite.Application.Courses;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeCourseQueries : ICourseQueries
{
    public List<CourseListItem> Courses { get; } = [];

    public List<CourseDetails> Details { get; } = [];

    public Task<IReadOnlyList<CourseListItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CourseListItem>>(
            [.. Courses.Where(course => (category is null || course.Category == category) && (isActive is null || course.IsActive == isActive))]);

    public Task<CourseDetails?> GetAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Details.SingleOrDefault(course => course.Id == courseId));
}
