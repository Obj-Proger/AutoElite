using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StaffTasks.GetMyStaffTasks;

/// <summary>Checks the status and the page.</summary>
internal sealed class GetMyStaffTasksQueryValidator : PagedQueryValidator<GetMyStaffTasksQuery>
{
    public GetMyStaffTasksQueryValidator()
    {
        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.StaffTask.StatusNotDefined);
    }
}
