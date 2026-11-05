using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StaffTasks.GetStaffTasks;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetStaffTasksQueryValidator : PagedQueryValidator<GetStaffTasksQuery>
{
    public GetStaffTasksQueryValidator()
    {
        RuleFor(query => query.AssigneeId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StaffTask.AssigneeIdEmpty);

        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.StaffTask.StatusNotDefined);

        RuleFor(query => query.SubjectKind)
            .IsInEnum()
            .WithError(DomainErrors.EntityReference.KindNotDefined);

        RuleFor(query => query.SubjectId)
            .NotNull()
            .When(query => query.SubjectKind is not null)
            .WithError(ApplicationErrors.StaffTask.SubjectIncomplete);

        RuleFor(query => query.SubjectKind)
            .NotNull()
            .When(query => query.SubjectId is not null)
            .WithError(ApplicationErrors.StaffTask.SubjectIncomplete);

        RuleFor(query => query.SubjectId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.EntityReference.IdEmpty);
    }
}
