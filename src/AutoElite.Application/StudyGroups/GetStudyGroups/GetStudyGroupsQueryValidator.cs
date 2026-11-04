using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StudyGroups.GetStudyGroups;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetStudyGroupsQueryValidator : PagedQueryValidator<GetStudyGroupsQuery>
{
    public GetStudyGroupsQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StudyGroup.BranchIdEmpty);
    }
}
