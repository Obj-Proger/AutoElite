using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users.GetUsers;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    private string[] ErrorCodes(GetUsersQuery query) =>
        [.. _validator.Validate(query).Errors.Select(failure => failure.ErrorCode)];

    [Fact]
    public void Validate_Defaults_Pass()
    {
        ErrorCodes(new GetUsersQuery()).Should().BeEmpty();
    }

    [Fact]
    public void Validate_SearchAtTheLimitWithSpaces_Passes()
    {
        ErrorCodes(new GetUsersQuery($"  {new string('a', PageRequest.MaxSearchLength)}  ")).Should().BeEmpty();
    }

    [Fact]
    public void Validate_SearchTooLong_ReportsSearchTooLong()
    {
        ErrorCodes(new GetUsersQuery(new string('a', PageRequest.MaxSearchLength + 1)))
            .Should().Equal(ApplicationErrors.Paging.SearchTooLong.Code);
    }

    [Fact]
    public void Validate_InvalidFilters_ReportsEach()
    {
        ErrorCodes(new GetUsersQuery(Role: (UserRole)99, BranchId: Guid.Empty, PageSize: 0)).Should().BeEquivalentTo(
            new[]
            {
                ApplicationErrors.Paging.PageSizeOutOfRange.Code,
                DomainErrors.UserRoleAssignment.RoleNotDefined.Code,
                DomainErrors.User.BranchIdEmpty.Code
            });
    }
}
