using AutoElite.Application.Users.Logout;

namespace AutoElite.Application.Tests.Users;

public class LogoutCommandHandlerTests
{
    private readonly FakeRefreshTokenStore _refreshTokens = new();

    private Task<Result> Handle(string? token) =>
        new LogoutCommandHandler(_refreshTokens, TestData.Clock())
            .Handle(new LogoutCommand(token), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_SessionToken_RevokesIt()
    {
        var token = await _refreshTokens.IssueAsync(Guid.CreateVersion7(), TestData.Now, TestContext.Current.CancellationToken);

        var result = await Handle(token.Value);

        result.IsSuccess.Should().BeTrue();
        _refreshTokens.Tokens[token.Value].IsRevoked.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Handle_NoOrUnknownToken_Succeeds(string? token)
    {
        var result = await Handle(token);

        result.IsSuccess.Should().BeTrue();
    }
}
