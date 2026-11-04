using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Vehicles.GetVehicles;

/// <summary>Checks the filters.</summary>
internal sealed class GetVehiclesQueryValidator : AbstractValidator<GetVehiclesQuery>
{
    public GetVehiclesQueryValidator()
    {
        RuleFor(query => query.Category)
            .IsInEnum()
            .WithError(DomainErrors.Vehicle.CategoryNotDefined);

        RuleFor(query => query.Transmission)
            .IsInEnum()
            .WithError(DomainErrors.Vehicle.TransmissionNotDefined);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Vehicle.BranchIdEmpty);
    }
}
