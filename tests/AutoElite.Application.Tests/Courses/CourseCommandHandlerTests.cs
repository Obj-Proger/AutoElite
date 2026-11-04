using AutoElite.Application.Courses;
using AutoElite.Application.Courses.CreateCourse;
using AutoElite.Application.Courses.DeactivateCourse;
using AutoElite.Application.Courses.ReactivateCourse;
using AutoElite.Application.Courses.UpdateCourse;
using AutoElite.Application.Settings;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Courses;

public class CourseCommandHandlerTests
{
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeSchoolSettingsRepository _settings = new();

    private CoursePrice Price() => new(new SchoolSettingsProvider(_settings));

    private Task<Result<Guid>> Create(CreateCourseCommand command) =>
        new CreateCourseCommandHandler(_courses, Price()).Handle(command, TestContext.Current.CancellationToken);

    private Task<Result> Update(UpdateCourseCommand command) =>
        new UpdateCourseCommandHandler(_courses, Price()).Handle(command, TestContext.Current.CancellationToken);

    private static CreateCourseCommand CreateCommand(string? currency = null) =>
        new("Category B", LicenseCategory.B, 45000m, currency, 134, 56, "Full course");

    [Fact]
    public async Task Create_WithoutCurrency_UsesTheSchoolsDefaultCurrency()
    {
        _settings.Settings = SchoolSettings.CreateDefault(Currency.Create("KZT").Value);

        var result = await Create(CreateCommand());

        var course = _courses.Items.Should().ContainSingle().Which;
        course.Id.Should().Be(result.Value);
        course.Price.Should().Be(Money.Create(45000m, Currency.Create("KZT").Value).Value);
        course.Category.Should().Be(LicenseCategory.B);
        course.TheoryHours.Should().Be(134);
        course.PracticeHours.Should().Be(56);
        course.Description.Should().Be("Full course");
    }

    [Fact]
    public async Task Create_WithCurrency_UsesIt()
    {
        await Create(CreateCommand("usd"));

        _courses.Items.Single().Price.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public async Task Create_NoHours_ReturnsTheDomainError()
    {
        var result = await Create(CreateCommand() with { TheoryHours = 0, PracticeHours = 0 });

        result.Error.Should().Be(DomainErrors.Course.NoHours);
        _courses.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_PriceWithTooManyDecimals_ReturnsTheMoneyError()
    {
        var result = await Create(CreateCommand("RUB") with { PriceAmount = 100.123m });

        result.Error.Should().Be(DomainErrors.Money.TooManyDecimalPlaces);
    }

    [Fact]
    public async Task Update_ExistingCourse_AppliesTheDetails()
    {
        var course = TestData.NewCourse();
        _courses.Seed(course);

        var result = await Update(new UpdateCourseCommand(course.Id, "Category A", LicenseCategory.A, 30000m, "RUB", 100, 20, null));

        result.IsSuccess.Should().BeTrue();
        course.Name.Should().Be("Category A");
        course.Category.Should().Be(LicenseCategory.A);
        course.Price.Amount.Should().Be(30000m);
        course.Description.Should().BeNull();
    }

    [Fact]
    public async Task Update_UnknownCourse_ReturnsCourseNotFound()
    {
        var result = await Update(new UpdateCourseCommand(Guid.CreateVersion7(), "Category A", LicenseCategory.A, 30000m, null, 100, 20, null));

        result.Error.Should().Be(ApplicationErrors.NotFound<Course>());
    }

    [Fact]
    public async Task Update_InactiveCourse_ReturnsCannotModifyInactive()
    {
        var course = TestData.NewCourse();
        course.Deactivate();
        _courses.Seed(course);

        var result = await Update(new UpdateCourseCommand(course.Id, "Category A", LicenseCategory.A, 30000m, null, 100, 20, null));

        result.Error.Should().Be(DomainErrors.Course.CannotModifyInactive);
    }

    [Fact]
    public async Task DeactivateAndReactivate_ToggleTheCourse()
    {
        var course = TestData.NewCourse();
        _courses.Seed(course);

        var deactivation = await new DeactivateCourseCommandHandler(_courses)
            .Handle(new DeactivateCourseCommand(course.Id), TestContext.Current.CancellationToken);
        var inactiveAfterDeactivation = !course.IsActive;
        var reactivation = await new ReactivateCourseCommandHandler(_courses)
            .Handle(new ReactivateCourseCommand(course.Id), TestContext.Current.CancellationToken);

        deactivation.IsSuccess.Should().BeTrue();
        inactiveAfterDeactivation.Should().BeTrue();
        reactivation.IsSuccess.Should().BeTrue();
        course.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Validator_UndefinedCategoryAndInvalidCurrency_ReportsBoth()
    {
        var result = new CreateCourseCommandValidator().Validate(CreateCommand("rubles") with { Category = (LicenseCategory)99 });

        result.Errors.Select(failure => failure.ErrorCode)
            .Should().Equal(DomainErrors.Course.CategoryNotDefined.Code, DomainErrors.Currency.InvalidFormat.Code);
    }
}
