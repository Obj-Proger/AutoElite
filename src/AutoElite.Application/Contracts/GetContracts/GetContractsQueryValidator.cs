using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Contracts.GetContracts;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetContractsQueryValidator : PagedQueryValidator<GetContractsQuery>
{
    public GetContractsQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.Contract.StatusNotDefined);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Contract.BranchIdEmpty);
    }
}
