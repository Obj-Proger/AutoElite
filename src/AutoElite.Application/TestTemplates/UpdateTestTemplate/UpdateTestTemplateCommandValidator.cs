using FluentValidation;

namespace AutoElite.Application.TestTemplates.UpdateTestTemplate;

/// <summary>Checks the category, the source and that there are blocks.</summary>
internal sealed class UpdateTestTemplateCommandValidator : AbstractValidator<UpdateTestTemplateCommand>
{
    public UpdateTestTemplateCommandValidator()
    {
        TestTemplateContent.AddRules(this);
    }
}
