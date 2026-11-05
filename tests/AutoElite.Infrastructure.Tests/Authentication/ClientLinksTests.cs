using AutoElite.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.Authentication;

public sealed class ClientLinksTests
{
    private static readonly Guid _userId = Guid.Parse("0190f0c4-0000-7000-8000-000000000001");

    private static ClientLinks Links(string baseUrl) => new(Options.Create(new ClientOptions { BaseUrl = new Uri(baseUrl) }));

    [Fact]
    public void Invitation_EscapesTheTokenAndKeepsTheBasePath()
    {
        var link = Links("https://school.example.ru/app").Invitation(_userId, "a+b/c=");

        link.AbsoluteUri.Should().Be(
            $"https://school.example.ru/app/{ClientLinks.InvitationPage}?userId={_userId}&token=a%2Bb%2Fc%3D");
    }

    [Fact]
    public void EmailChange_CarriesTheNewAddress()
    {
        var link = Links("https://school.example.ru/").EmailChange(_userId, Email.Create("new+mail@example.com").Value, "t");

        link.AbsoluteUri.Should().Be(
            $"https://school.example.ru/{ClientLinks.EmailChangePage}?userId={_userId}&email=new%2Bmail%40example.com&token=t");
    }

    [Fact]
    public void PasswordReset_PointsToTheResetPage()
    {
        var link = Links("https://school.example.ru/").PasswordReset(_userId, "t");

        link.AbsolutePath.Should().Be($"/{ClientLinks.PasswordResetPage}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ftp://school.example.ru/")]
    public void Settings_WithoutAWebAddress_HaveAProblem(string? baseUrl)
    {
        var options = new ClientOptions { BaseUrl = baseUrl is null ? null : new Uri(baseUrl) };

        options.Problems().Should().ContainSingle();
    }
}
