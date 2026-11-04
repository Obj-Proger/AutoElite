namespace AutoElite.Application.Courses;

/// <summary>A course in the catalog list.</summary>
/// <param name="Id">The identifier of the course.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category the course prepares for.</param>
/// <param name="PriceAmount">The price.</param>
/// <param name="PriceCurrency">The ISO 4217 code of the price currency.</param>
/// <param name="TheoryHours">The theory hours.</param>
/// <param name="PracticeHours">The practice hours.</param>
/// <param name="TopicCount">The number of topics in the curriculum.</param>
/// <param name="IsActive">Whether new contracts and groups may use the course.</param>
public sealed record CourseListItem(
    Guid Id,
    string Name,
    LicenseCategory Category,
    decimal PriceAmount,
    string PriceCurrency,
    int TheoryHours,
    int PracticeHours,
    int TopicCount,
    bool IsActive);

/// <summary>A topic of a course curriculum.</summary>
/// <param name="Id">The identifier of the topic.</param>
/// <param name="Position">The one-based position in the curriculum.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description, if any.</param>
public sealed record CourseTopicItem(Guid Id, int Position, string Title, string? Description);

/// <summary>A course with its curriculum.</summary>
/// <param name="Id">The identifier of the course.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The licence category the course prepares for.</param>
/// <param name="PriceAmount">The price.</param>
/// <param name="PriceCurrency">The ISO 4217 code of the price currency.</param>
/// <param name="TheoryHours">The theory hours.</param>
/// <param name="PracticeHours">The practice hours.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="IsActive">Whether new contracts and groups may use the course.</param>
/// <param name="Topics">The topics in curriculum order.</param>
public sealed record CourseDetails(
    Guid Id,
    string Name,
    LicenseCategory Category,
    decimal PriceAmount,
    string PriceCurrency,
    int TheoryHours,
    int PracticeHours,
    string? Description,
    bool IsActive,
    IReadOnlyList<CourseTopicItem> Topics);
