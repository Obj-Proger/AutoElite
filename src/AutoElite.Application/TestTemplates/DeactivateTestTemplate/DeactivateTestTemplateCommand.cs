namespace AutoElite.Application.TestTemplates.DeactivateTestTemplate;

/// <summary>Stops offering a template for new tests; tests already taken keep it.</summary>
/// <param name="TemplateId">The identifier of the template.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record DeactivateTestTemplateCommand(Guid TemplateId) : ICommand;
