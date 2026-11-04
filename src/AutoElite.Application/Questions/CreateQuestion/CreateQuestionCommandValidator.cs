using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Questions.CreateQuestion;

/// <summary>
/// Checks the source and the categories. The texts, the options and the correct answer are checked by the domain,
/// whose error codes name the field.
/// </summary>
internal sealed class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(command => command.Source)
            .IsInEnum()
            .WithError(DomainErrors.Question.SourceNotDefined);

        RuleFor(command => command.Categories)
            .NotEmpty()
            .WithError(DomainErrors.Question.NoCategories);

        RuleForEach(command => command.Categories!)
            .IsInEnum()
            .WithError(DomainErrors.Question.CategoryNotDefined);
    }
}
