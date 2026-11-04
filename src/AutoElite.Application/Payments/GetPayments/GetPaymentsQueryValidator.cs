using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Payments.GetPayments;

/// <summary>Checks the period, the filters and the page.</summary>
internal sealed class GetPaymentsQueryValidator : PagedQueryValidator<GetPaymentsQuery>
{
    public GetPaymentsQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.To)
            .Must((query, to) => query.From is null || to is null || to >= query.From)
            .WithError(ApplicationErrors.Payment.PeriodInvalid);

        RuleFor(query => query.Method)
            .IsInEnum()
            .WithError(DomainErrors.Payment.MethodNotDefined);

        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.Payment.StatusNotDefined);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Contract.BranchIdEmpty);
    }
}
