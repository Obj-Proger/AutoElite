using AutoElite.Application.Common.Abstractions;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Abstractions;

public class CurrentUserExtensionsTests
{
    [Fact]
    public void IsAuthenticated_SignedInUser_ReturnsTrue()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Student);

        currentUser.IsAuthenticated().Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_AnonymousCaller_ReturnsFalse()
    {
        FakeCurrentUser.Anonymous().IsAuthenticated().Should().BeFalse();
    }

    [Fact]
    public void GetRequiredUserId_SignedInUser_ReturnsTheUserId()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Manager);

        currentUser.GetRequiredUserId().Should().Be(currentUser.UserId!.Value);
    }

    [Fact]
    public void GetRequiredUserId_AnonymousCaller_Throws()
    {
        var act = () => FakeCurrentUser.Anonymous().GetRequiredUserId();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsInRole_UserWithTheRole_ReturnsTrue()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Student, UserRole.Manager);

        currentUser.IsInRole(UserRole.Manager).Should().BeTrue();
    }

    [Fact]
    public void IsInRole_UserWithoutTheRole_ReturnsFalse()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Student);

        currentUser.IsInRole(UserRole.Administrator).Should().BeFalse();
    }

    [Fact]
    public void IsInAnyRole_UserWithOneOfTheRoles_ReturnsTrue()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Manager);

        currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager).Should().BeTrue();
    }

    [Fact]
    public void IsInAnyRole_UserWithNoneOfTheRoles_ReturnsFalse()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Student);

        currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager).Should().BeFalse();
    }

    [Fact]
    public void IsInAnyRole_NoRolesGiven_ReturnsFalse()
    {
        var currentUser = FakeCurrentUser.SignedIn(UserRole.Student);

        currentUser.IsInAnyRole().Should().BeFalse();
    }
}
