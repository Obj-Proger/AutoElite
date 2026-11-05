using AutoElite.Application.Common.Authorization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Authorization;

public class AuthorizationRequirementTests
{
    [Fact]
    public void For_UnmarkedRequest_RequiresAnySignedInUser()
    {
        var requirement = AuthorizationRequirement.For(typeof(TestCommand));

        requirement.AllowsAnonymous.Should().BeFalse();
        requirement.Roles.Should().BeEmpty();
        requirement.SystemOnly.Should().BeFalse();
    }

    [Fact]
    public void For_SystemRequest_IsSystemOnly()
    {
        var requirement = AuthorizationRequirement.For(typeof(SystemCommand));

        requirement.SystemOnly.Should().BeTrue();
        requirement.AllowsAnonymous.Should().BeFalse();
        requirement.Roles.Should().BeEmpty();
    }

    [Fact]
    public void For_SystemRequestWithRoles_Throws()
    {
        var act = () => AuthorizationRequirement.For(typeof(SystemAndRolesCommand));

        act.Should().Throw<InvalidOperationException>().WithMessage("*SystemAndRolesCommand*");
    }

    [Fact]
    public void For_AnonymousRequest_AllowsAnonymousCallers()
    {
        var requirement = AuthorizationRequirement.For(typeof(PublicCommand));

        requirement.AllowsAnonymous.Should().BeTrue();
        requirement.Roles.Should().BeEmpty();
    }

    [Fact]
    public void For_RoleRequest_ReturnsTheRoles()
    {
        var requirement = AuthorizationRequirement.For(typeof(ManagerCommand));

        requirement.AllowsAnonymous.Should().BeFalse();
        requirement.Roles.Should().Equal(UserRole.Manager, UserRole.Administrator);
    }

    [Fact]
    public void For_RequestMarkedAnonymousAndWithRoles_Throws()
    {
        var request = new ContradictoryCommand();

        var act = () => AuthorizationRequirement.For(request.GetType());

        act.Should().Throw<InvalidOperationException>().WithMessage("*ContradictoryCommand*");
    }
}
