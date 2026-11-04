namespace AutoElite.Application.Courses.CreateCourse;

/// <summary>Adds a course to the catalog (FR-3.1); topics are added to it afterwards.</summary>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category the course prepares for.</param>
/// <param name="PriceAmount">The price, greater than zero.</param>
/// <param name="PriceCurrency">The ISO 4217 code of the price currency; empty for the school's default currency.</param>
/// <param name="TheoryHours">The theory hours, from 0 to 1000.</param>
/// <param name="PracticeHours">The practice hours, from 0 to 1000; the course needs theory or practice hours.</param>
/// <param name="Description">An optional description.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record CreateCourseCommand(
    string? Name,
    LicenseCategory Category,
    decimal PriceAmount,
    string? PriceCurrency,
    int TheoryHours,
    int PracticeHours,
    string? Description) : ICommand<Guid>;
