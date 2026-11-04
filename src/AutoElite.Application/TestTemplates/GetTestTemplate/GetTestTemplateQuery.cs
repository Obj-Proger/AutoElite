namespace AutoElite.Application.TestTemplates.GetTestTemplate;

/// <summary>Reads the card of a template with its rules and blocks. Students see only active templates.</summary>
/// <param name="TemplateId">The identifier of the template.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor, UserRole.Student)]
public sealed record GetTestTemplateQuery(Guid TemplateId) : IQuery<TestTemplateDetails>;
