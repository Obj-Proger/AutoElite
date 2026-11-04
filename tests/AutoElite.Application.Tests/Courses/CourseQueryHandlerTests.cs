using AutoElite.Application.Courses;
using AutoElite.Application.Courses.GetCourse;
using AutoElite.Application.Courses.GetCourses;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Courses;

public class CourseQueryHandlerTests
{
    private readonly FakeCourseQueries _queries = new();

    private static CourseListItem Item(string name, LicenseCategory category) =>
        new(Guid.CreateVersion7(), name, category, 45000m, "RUB", 134, 56, 10, true);

    [Fact]
    public async Task GetCourses_CategoryFilter_PassesIt()
    {
        var categoryB = Item("Category B", LicenseCategory.B);
        _queries.Courses.Add(categoryB);
        _queries.Courses.Add(Item("Category A", LicenseCategory.A));

        var result = await new GetCoursesQueryHandler(_queries)
            .Handle(new GetCoursesQuery(LicenseCategory.B), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(categoryB);
    }

    [Fact]
    public async Task GetCourse_UnknownCourse_ReturnsCourseNotFound()
    {
        var result = await new GetCourseQueryHandler(_queries)
            .Handle(new GetCourseQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Course>());
    }

    [Fact]
    public async Task GetCourse_ExistingCourse_ReturnsItWithTopics()
    {
        var details = new CourseDetails(
            Guid.CreateVersion7(), "Category B", LicenseCategory.B, 45000m, "RUB", 134, 56, null, true,
            [new CourseTopicItem(Guid.CreateVersion7(), 1, "Road signs", null)]);
        _queries.Details.Add(details);

        var result = await new GetCourseQueryHandler(_queries)
            .Handle(new GetCourseQuery(details.Id), TestContext.Current.CancellationToken);

        result.Value.Should().BeSameAs(details);
    }
}
