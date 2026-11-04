using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Leads.GetLeads;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetLeadsQueryValidator : PagedQueryValidator<GetLeadsQuery>
{
    public GetLeadsQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.Lead.StatusNotDefined);

        RuleFor(query => query.Source)
            .IsInEnum()
            .WithError(DomainErrors.Lead.SourceNotDefined);

        RuleFor(query => query.ManagerId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.ManagerIdEmpty);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.BranchIdEmpty);
    }
}
