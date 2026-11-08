using System.Security.Claims;
using AutoElite.API.Authentication;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace AutoElite.API.Tests.Authentication;

public sealed class HttpCurrentUserTests
{
    private static readonly UserRole[] _managerAndStudent = [UserRole.Manager, UserRole.Student];

    private static HttpCurrentUser CurrentUser(HttpContext? context) =>
        new(new HttpContextAccessor { HttpContext = context });

    private static DefaultHttpContext SignedIn(params Claim[] claims) =>
        new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer")) };

    [Fact]
    public void WithoutARequest_IsTheSystem()
    {
        var user = CurrentUser(null);

        user.IsSystem.Should().BeTrue();
        user.UserId.Should().BeNull();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void AnonymousRequest_IsNeitherTheSystemNorAUser()
    {
        var user = CurrentUser(new DefaultHttpContext());

        user.IsSystem.Should().BeFalse();
        user.UserId.Should().BeNull();
        user.BranchId.Should().BeNull();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void SignedInRequest_IsTheUserOfTheToken_WithTheRolesItKnows()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var context = SignedIn(
            new Claim(AccessTokenClaims.UserId, userId.ToString()),
            new Claim(AccessTokenClaims.BranchId, branchId.ToString()),
            new Claim(AccessTokenClaims.Role, nameof(UserRole.Manager)),
            new Claim(AccessTokenClaims.Role, nameof(UserRole.Student)),
            new Claim(AccessTokenClaims.Role, "Janitor"));

        var user = CurrentUser(context);

        user.IsSystem.Should().BeFalse();
        user.UserId.Should().Be(userId);
        user.BranchId.Should().Be(branchId);
        user.Roles.Should().BeEquivalentTo(_managerAndStudent);
    }

    [Fact]
    public void TokenWithoutAUserId_IsAnonymous()
    {
        var user = CurrentUser(SignedIn(new Claim(AccessTokenClaims.Role, nameof(UserRole.Administrator))));

        user.UserId.Should().BeNull();
        user.Roles.Should().BeEmpty();
    }
}
