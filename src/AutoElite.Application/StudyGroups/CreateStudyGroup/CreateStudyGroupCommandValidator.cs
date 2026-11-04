using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StudyGroups.CreateStudyGroup;

/// <summary>Checks the branch identifier; the name, dates and capacity are checked by the domain.</summary>
internal sealed class CreateStudyGroupCommandValidator : AbstractValidator<CreateStudyGroupCommand>
{
    public CreateStudyGroupCommandValidator()
    {
        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StudyGroup.BranchIdEmpty);
    }
}
