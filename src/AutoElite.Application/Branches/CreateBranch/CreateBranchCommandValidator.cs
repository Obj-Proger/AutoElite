using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Branches.CreateBranch;

/// <summary>Checks the details of a new branch.</summary>
internal sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(command => command.Name).ValidBranchName();
        RuleFor(command => command.City).ValidCity();
        RuleFor(command => command.Street).ValidStreet();
        RuleFor(command => command.TimeZoneId).ValidTimeZone();

        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
    }
}
