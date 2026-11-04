using FluentValidation;

namespace AutoElite.Application.TestTemplates.CreateTestTemplate;

/// <summary>Checks the category, the source and that there are blocks.</summary>
internal sealed class CreateTestTemplateCommandValidator : AbstractValidator<CreateTestTemplateCommand>
{
    public CreateTestTemplateCommandValidator()
    {
        TestTemplateContent.AddRules(this);
    }
}
