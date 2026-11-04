namespace AutoElite.Application.Courses;

/// <summary>Reads the course catalog for queries.</summary>
public interface ICourseQueries
{
    /// <summary>
    /// Reads the courses that match the conditions, sorted by category and name. A catalog is small, so the list is
    /// not paged.
    /// </summary>
    /// <param name="category">Only courses for this category; <see langword="null"/> for all.</param>
    /// <param name="isActive">Only active or only deactivated courses; <see langword="null"/> for all.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<CourseListItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>Reads one course with its curriculum.</summary>
    /// <param name="courseId">The identifier of the course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The course, or <see langword="null"/> if it does not exist.</returns>
    Task<CourseDetails?> GetAsync(Guid courseId, CancellationToken cancellationToken = default);
}
