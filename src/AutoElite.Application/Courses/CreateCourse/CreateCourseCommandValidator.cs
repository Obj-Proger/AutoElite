using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Courses.CreateCourse;

/// <summary>
/// Checks the category and the currency. The name, hours, description and price are checked by the domain, whose
/// error codes name the field.
/// </summary>
internal sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(command => command.Category)
            .IsInEnum()
            .WithError(DomainErrors.Course.CategoryNotDefined);

        When(
            command => !string.IsNullOrWhiteSpace(command.PriceCurrency),
            () => RuleFor(command => command.PriceCurrency).ValidatedBy(Currency.Create));
    }
}
