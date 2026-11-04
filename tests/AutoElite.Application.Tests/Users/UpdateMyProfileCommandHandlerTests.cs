using AutoElite.Application.Users.UpdateMyProfile;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class UpdateMyProfileCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();

    private static readonly UpdateMyProfileCommand _command =
        new("Petrova", "Anna", "Sergeevna", "+79991234567", Language.English);

    private Task<Result> Handle(User user, UpdateMyProfileCommand command) =>
        new UpdateMyProfileCommandHandler(_users, new FakeCurrentUser { UserId = user.Id })
            .Handle(command, TestContext.Current.CancellationToken);

    private User SeedUser()
    {
        var user = TestData.NewUser();
        _users.Seed(user);

        return user;
    }

    [Fact]
    public async Task Handle_ValidDetails_UpdatesNamePhoneAndLanguage()
    {
        var user = SeedUser();

        var result = await Handle(user, _command);

        result.IsSuccess.Should().BeTrue();
        user.FullName.Should().Be(FullName.Create("Petrova", "Anna", "Sergeevna").Value);
        user.Phone!.Value.Should().Be("+79991234567");
        user.PreferredLanguage.Should().Be(Language.English);
    }

    [Fact]
    public async Task Handle_EmptyPhoneAndMiddleName_RemovesThem()
    {
        var user = SeedUser();
        await Handle(user, _command);

        await Handle(user, _command with { MiddleName = "", Phone = " " });

        user.FullName.MiddleName.Should().BeNull();
        user.Phone.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsCannotModifyInactive()
    {
        var user = SeedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle(user, _command);

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsUserNotFound()
    {
        var result = await Handle(TestData.NewUser(), _command);

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }
}
