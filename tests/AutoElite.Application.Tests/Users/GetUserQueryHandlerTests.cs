using AutoElite.Application.Users;
using AutoElite.Application.Users.GetUser;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class GetUserQueryHandlerTests
{
    private readonly FakeUserQueries _queries = new();

    private Task<Result<UserDetails>> Handle(Guid userId, FakeCurrentUser caller) =>
        new GetUserQueryHandler(_queries, caller).Handle(new GetUserQuery(userId), TestContext.Current.CancellationToken);

    private UserDetails SeedDetails(Guid? branchId)
    {
        var details = new UserDetails(
            Guid.CreateVersion7(), "Ivanov", "Ivan", null, "ivan@example.com", null, null, branchId, null,
            Language.Russian, true, false, TestData.Now, [UserRole.Student]);
        _queries.Seed(details);

        return details;
    }

    [Fact]
    public async Task Handle_UserOfManagersBranch_ReturnsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = SeedDetails(branchId);

        var result = await Handle(details.Id, TestData.Manager(branchId));

        result.Value.Should().Be(details);
    }

    [Fact]
    public async Task Handle_UserWithoutBranch_IsVisibleToEveryManager()
    {
        var details = SeedDetails(null);

        var result = await Handle(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserOfAnotherBranch_ReturnsUserNotFoundToTheManager()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await Handle(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsUserNotFound()
    {
        var result = await Handle(Guid.CreateVersion7(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }
}
