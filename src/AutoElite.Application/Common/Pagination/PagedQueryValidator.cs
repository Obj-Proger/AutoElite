using System.Linq.Expressions;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Common.Pagination;

/// <summary>
/// The base of every paged query's validator: checks the page number and size. Derived validators add the rules
/// of their own filters in the constructor.
/// </summary>
/// <typeparam name="TQuery">The type of the paged query.</typeparam>
public abstract class PagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IPagedQuery
{
    /// <summary>Initializes a new instance with the paging rules.</summary>
    protected PagedQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, PageRequest.MaxPage)
            .WithError(ApplicationErrors.Paging.PageOutOfRange);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithError(ApplicationErrors.Paging.PageSizeOutOfRange);
    }

    /// <summary>Adds the rule for the optional search text of a list: at most <see cref="PageRequest.MaxSearchLength"/> characters.</summary>
    /// <param name="search">The search text property.</param>
    protected void RuleForSearch(Expression<Func<TQuery, string?>> search)
    {
        RuleFor(search)
            .Must(text => text is null || text.Trim().Length <= PageRequest.MaxSearchLength)
            .WithError(ApplicationErrors.Paging.SearchTooLong);
    }
}
