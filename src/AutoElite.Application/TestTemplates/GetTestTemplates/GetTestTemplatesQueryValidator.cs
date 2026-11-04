using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TestTemplates.GetTestTemplates;

/// <summary>Checks the category filter.</summary>
internal sealed class GetTestTemplatesQueryValidator : AbstractValidator<GetTestTemplatesQuery>
{
    public GetTestTemplatesQueryValidator()
    {
        RuleFor(query => query.Category)
            .IsInEnum()
            .WithError(DomainErrors.TestTemplate.CategoryNotDefined);
    }
}
