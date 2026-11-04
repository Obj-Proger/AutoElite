using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.GetUsers;

/// <summary>Checks the filters and the page.</summary>
internal sealed class GetUsersQueryValidator : PagedQueryValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleForSearch(query => query.Search);

        RuleFor(query => query.Role)
            .IsInEnum()
            .WithError(DomainErrors.UserRoleAssignment.RoleNotDefined);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.User.BranchIdEmpty);
    }
}
