using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Courses.GetCourses;

/// <summary>Checks that the category filter, if given, is defined.</summary>
internal sealed class GetCoursesQueryValidator : AbstractValidator<GetCoursesQuery>
{
    public GetCoursesQueryValidator()
    {
        RuleFor(query => query.Category)
            .IsInEnum()
            .WithError(DomainErrors.Course.CategoryNotDefined);
    }
}
