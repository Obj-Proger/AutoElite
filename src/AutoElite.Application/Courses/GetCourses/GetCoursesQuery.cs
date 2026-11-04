namespace AutoElite.Application.Courses.GetCourses;

/// <summary>Reads the course catalog. Every signed-in user may read it.</summary>
/// <param name="Category">Only courses for this category.</param>
/// <param name="IsActive">Only active or only withdrawn courses.</param>
public sealed record GetCoursesQuery(LicenseCategory? Category = null, bool? IsActive = null) : IQuery<IReadOnlyList<CourseListItem>>;
