using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users;
using AutoElite.Application.Users.GetUsers;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class GetUsersQueryHandlerTests
{
    private readonly FakeUserQueries _queries = new();

    private Task<Result<PagedList<UserListItem>>> Handle(GetUsersQuery query, FakeCurrentUser caller) =>
        new GetUsersQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Filters_PassesThemWithTrimmedSearchScopeAndPage()
    {
        var branchId = Guid.CreateVersion7();
        var query = new GetUsersQuery("  Ivan  ", UserRole.Student, branchId, true, 2, 50);

        var result = await Handle(query, TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        _queries.LastListCall.Should().Be((
            new UserListFilter("Ivan", UserRole.Student, branchId, true), DataScope.SchoolWide, new PageRequest(2, 50)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Handle_NoSearchText_PassesNoSearch(string? search)
    {
        await Handle(new GetUsersQuery(search), TestData.Administrator());

        _queries.LastListCall!.Value.Filter.Search.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Manager_ReadsWithinTheBranchScope()
    {
        var branchId = Guid.CreateVersion7();

        await Handle(new GetUsersQuery(), TestData.Manager(branchId));

        _queries.LastListCall!.Value.Scope.Should().Be(DataScope.ForBranch(branchId));
    }

    [Fact]
    public async Task Handle_ManagerFiltersByAnotherBranch_ReturnsAnEmptyPageWithoutReading()
    {
        var result = await Handle(
            new GetUsersQuery(BranchId: Guid.CreateVersion7(), PageSize: 10), TestData.Manager(Guid.CreateVersion7()));

        result.Value.Items.Should().BeEmpty();
        result.Value.PageSize.Should().Be(10);
        _queries.LastListCall.Should().BeNull();
    }
}
