using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Branches.UpdateBranch;

/// <summary>Checks the new details of a branch.</summary>
internal sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(command => command.Name).ValidBranchName();
        RuleFor(command => command.City).ValidCity();
        RuleFor(command => command.Street).ValidStreet();
        RuleFor(command => command.TimeZoneId).ValidTimeZone();

        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
    }
}
