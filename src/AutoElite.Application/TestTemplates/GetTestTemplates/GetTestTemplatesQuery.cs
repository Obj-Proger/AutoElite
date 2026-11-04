namespace AutoElite.Application.TestTemplates.GetTestTemplates;

/// <summary>
/// Reads the test templates sorted by name. Students see the active templates, to choose a practice test.
/// </summary>
/// <param name="Category">Only templates of this licence category.</param>
/// <param name="IsActive">Only active or only deactivated templates; ignored for students.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor, UserRole.Student)]
public sealed record GetTestTemplatesQuery(LicenseCategory? Category = null, bool? IsActive = null)
    : IQuery<IReadOnlyList<TestTemplateItem>>;
