using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TrainingGrounds.UpdateTrainingGround;

/// <summary>Checks the address of a changed ground. The name and the description are checked by the domain.</summary>
internal sealed class UpdateTrainingGroundCommandValidator : AbstractValidator<UpdateTrainingGroundCommand>
{
    public UpdateTrainingGroundCommandValidator()
    {
        RuleFor(command => command.City).ValidCity();
        RuleFor(command => command.Street).ValidStreet();
    }
}
