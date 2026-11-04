using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Courses.UpdateCourse;

/// <summary>Checks the category and the currency; the other details are checked by the domain.</summary>
internal sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(command => command.Category)
            .IsInEnum()
            .WithError(DomainErrors.Course.CategoryNotDefined);

        When(
            command => !string.IsNullOrWhiteSpace(command.PriceCurrency),
            () => RuleFor(command => command.PriceCurrency).ValidatedBy(Currency.Create));
    }
}
