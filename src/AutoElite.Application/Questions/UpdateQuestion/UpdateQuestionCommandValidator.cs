using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Questions.UpdateQuestion;

/// <summary>Checks the categories; the texts, the options and the correct answer are checked by the domain.</summary>
internal sealed class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    public UpdateQuestionCommandValidator()
    {
        RuleFor(command => command.Categories)
            .NotEmpty()
            .WithError(DomainErrors.Question.NoCategories);

        RuleForEach(command => command.Categories!)
            .IsInEnum()
            .WithError(DomainErrors.Question.CategoryNotDefined);
    }
}
