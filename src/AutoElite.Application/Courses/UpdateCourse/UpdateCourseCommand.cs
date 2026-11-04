namespace AutoElite.Application.Courses.UpdateCourse;

/// <summary>
/// Changes the details of an active course. Contracts already drawn up keep the price they were made with.
/// </summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category the course prepares for.</param>
/// <param name="PriceAmount">The price, greater than zero.</param>
/// <param name="PriceCurrency">The ISO 4217 code of the price currency; empty for the school's default currency.</param>
/// <param name="TheoryHours">The theory hours, from 0 to 1000.</param>
/// <param name="PracticeHours">The practice hours, from 0 to 1000.</param>
/// <param name="Description">An optional description; empty removes it.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateCourseCommand(
    Guid CourseId,
    string? Name,
    LicenseCategory Category,
    decimal PriceAmount,
    string? PriceCurrency,
    int TheoryHours,
    int PracticeHours,
    string? Description) : ICommand;
