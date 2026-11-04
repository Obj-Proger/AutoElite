namespace AutoElite.Application.TestTemplates.ReactivateTestTemplate;

/// <summary>Offers a template for new tests again.</summary>
/// <param name="TemplateId">The identifier of the template.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record ReactivateTestTemplateCommand(Guid TemplateId) : ICommand;
