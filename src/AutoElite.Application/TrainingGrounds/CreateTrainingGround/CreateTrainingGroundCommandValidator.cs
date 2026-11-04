using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TrainingGrounds.CreateTrainingGround;

/// <summary>Checks the address of a new ground. The name and the description are checked by the domain.</summary>
internal sealed class CreateTrainingGroundCommandValidator : AbstractValidator<CreateTrainingGroundCommand>
{
    public CreateTrainingGroundCommandValidator()
    {
        RuleFor(command => command.City).ValidCity();
        RuleFor(command => command.Street).ValidStreet();
    }
}
