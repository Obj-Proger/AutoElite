using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Questions.GetQuestions;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetQuestionsQueryValidator : PagedQueryValidator<GetQuestionsQuery>
{
    public GetQuestionsQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.Category)
            .IsInEnum()
            .WithError(DomainErrors.Question.CategoryNotDefined);

        RuleFor(query => query.Source)
            .IsInEnum()
            .WithError(DomainErrors.Question.SourceNotDefined);
    }
}
