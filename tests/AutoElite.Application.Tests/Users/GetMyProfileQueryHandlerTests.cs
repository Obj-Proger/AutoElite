using AutoElite.Application.Users;
using AutoElite.Application.Users.GetMyProfile;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class GetMyProfileQueryHandlerTests
{
    private readonly FakeUserQueries _queries = new();

    private Task<Result<UserProfile>> Handle(Guid userId) =>
        new GetMyProfileQueryHandler(_queries, new FakeCurrentUser { UserId = userId })
            .Handle(new GetMyProfileQuery(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsTheCallersProfile()
    {
        var profile = new UserProfile(
            Guid.CreateVersion7(), "Ivanov", "Ivan", null, "ivan@example.com", null, null, null, null,
            Language.Russian, [UserRole.Student]);
        _queries.Seed(profile);

        var result = await Handle(profile.Id);

        result.Value.Should().Be(profile);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsUserNotFound()
    {
        var result = await Handle(Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }
}
