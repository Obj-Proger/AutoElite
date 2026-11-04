using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TrainingGrounds.GetTrainingGrounds;

/// <summary>Checks the branch filter.</summary>
internal sealed class GetTrainingGroundsQueryValidator : AbstractValidator<GetTrainingGroundsQuery>
{
    public GetTrainingGroundsQueryValidator()
    {
        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.TrainingGround.BranchIdEmpty);
    }
}
