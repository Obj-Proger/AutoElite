using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.DrivingSlots.GetDrivingSchedule;

/// <summary>Checks the status filter; the period is checked by the handler.</summary>
internal sealed class GetDrivingScheduleQueryValidator : AbstractValidator<GetDrivingScheduleQuery>
{
    public GetDrivingScheduleQueryValidator()
    {
        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.DrivingSlot.StatusNotDefined);
    }
}
